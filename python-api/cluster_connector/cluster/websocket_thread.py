import asyncio
import threading
import websockets
import collections
import queue
import json
import logging


class WebsocketThread(threading.Thread):
    """Initiates a thread to run a websocket connection, send messages from a `reply_queue` and receive tasks, saving
        them in a `tasks_queue`.

    .. versionadded::0.2.0
    .. versionchanged::1.1.0

    Attributes:
        stop: A boolean controlling the running state of this thread. When stop is set to True, running tasks are
            interrupted, the websocket is closed if it was open and the `run()` method returns.
            When stop is set to True by a method of this thread, an exception is added to the `exception_queue` provided
            at initialisation.
    """

    __version__ = '1.1.0'

    def __init__(self, websocket_uri: str, exception_queue: queue.Queue, add_tasks,
                 reply_queue: collections.deque, loop, authorization, connection_timeout: float):
        """
        Args:
            websocket_uri: A string containing the uri of the websocket host with which a connection should be made.
            exception_queue: A reference to a queue in which raised exceptions should be saved to be passed on to the
                caller of this thread.
            add_tasks: An instance method of the calling class that handles received messages.
            reply_queue: A queue in which the messages to be sent can be found.
            loop: The loop to be set as the asynchronous event loop of this thread.
            authorization: The value to be set as the `Authorization` header value to be sent on websocket connection
                set up.
            connection_timeout: The timeout to be set when connecting to the websocket host.
        """
        threading.Thread.__init__(self)
        self._exception_queue = exception_queue  # reference to queue to store raised exceptions to sync with caller
        self._websocket_uri = websocket_uri  # the uri of the websocket with which a connection should be made
        self._add_tasks = add_tasks  # an instance method of the calling class that adds new tasks to its collection.
        self._reply_queue = reply_queue  # reference to the waiting replies queue
        self._connection_timeout = connection_timeout   # the timeout set for a websocket connection to be established
        self._websocket = None  # variable to reference the websocket connection
        self.stop = False  # variable controlling whether this thread should keep running or should return
        self._loop = loop  # reference to the current loop in which async methods should be called
        asyncio.set_event_loop(loop)

        self._authorization = authorization  # authorization value to authorize to the server
        self._headers = {"Authorization": self._authorization}

    def run(self):
        """Starts communication with the websocket host."""
        self._loop.run_until_complete(self._communicate_with_server())
        logging.debug("Thread " + self.getName() + " has stopped.")

    async def _replies_to_send(self):
        """Generates next reply to be sent and removes it from the reply queue."""
        try:
            yield self._reply_queue.popleft()
        except IndexError:
            raise StopAsyncIteration()

    async def _receive_handler(self):
        """Checks for new messages from server and processes them.

        Sets self.stop to True when the websocket raises a `ConnectionClosedError`.

        Returns: None if the websocket raises a `ConnectionClosedError`.

        Post: `self.stop` equals True.
        """
        while not self.stop:
            try:
                async for message in self._websocket:
                    await self._process_received_message(message)
            except websockets.exceptions.ConnectionClosedError as ex:
                logging.debug(ex)
                self._exception_queue.put(ex)  # pass exception to caller of this thread
                self.stop = True  # return method and stop this thread
            except Exception as e:
                logging.debug(e)

    async def _process_received_message(self, message):
        """Adds valid received tasks to waiting tasks queue."""
        logging.debug("Processing received message: " + str(message))
        self._add_tasks(message)

    async def _send_handler(self):
        """Sends replies from reply queue.

        Waits 0.5s if reply could not be sent. Waits 0.05s if no replies are available.

        Post: `self.stop` equals True
        """
        while not self.stop:
            try:
                async for reply in self._replies_to_send():
                    await self._websocket.send(reply)
                    logging.debug("Reply sent: " + str(reply))
            except StopAsyncIteration as e1:
                logging.debug(e1)
                logging.debug("No replies available.")
            except RuntimeError as e2:
                logging.debug(e2)
                logging.debug("Can't handle StopAsyncIteration.")
                await asyncio.sleep(0.05)
            except Exception as e:
                logging.debug("Reply not sent.")
                logging.exception(e)
                await asyncio.sleep(0.5)

    async def _handler(self):
        """Lets sender and receiver handlers work asynchronously."""
        receive_task = asyncio.ensure_future(
            self._receive_handler())
        send_task = asyncio.ensure_future(
            self._send_handler())
        done, pending = await asyncio.wait(
            [receive_task, send_task],
            return_when=asyncio.FIRST_COMPLETED,
        )

        for task in pending:
            task.cancel()
            logging.debug("Task cancelled: " + str(task))

    async def _communicate_with_server(self):
        """Keeps websocket connection running.

        Tries to connect to the websocket server with a timeout equal to `self._connection_timeout`. When the connection
        has been established a *Connection established* message is sent to the host.
        If `websockets.exceptions.InvalidMessage` is raised for the first time, the method waits 1.5s asynchronously and
        retries once afterwards. `self.stop` is set to True when `websockets.exceptions.InvalidMessage` occurs a
        second time or when another Exception is raised.

        Returns: None when `self.stop` equals True.

        Post: Raised exceptions in the `self._exception_queue`.
        """
        second_chance = False
        while not self.stop:
            try:
                if self._websocket is None or not self._websocket.open:
                    logging.debug("Websocket NOT connected. Trying to connect. " + str(self._connection_timeout) +
                                  "s timeout set.")
                    await asyncio.wait_for(self._connect_to_server(), self._connection_timeout)
                    # Send initialisation message to wake host.
                    await self._websocket.send(json.dumps({"msg": "Connection established."}))
                logging.debug("Connection established.")
                await self._handler()
            except websockets.exceptions.InvalidMessage as e:
                # Server responds, but not correctly: retry once
                logging.exception(e)
                if second_chance:
                    loggin.debug("Second attempt to connect to active host failed.")
                    self._exception_queue.put(e)
                    self.stop = True
                else:
                    logging.debug("Retry once to connect after invalid response message from host. Wait 1.5s.")
                    await asyncio.sleep(1.5)
                second_chance = True
            except Exception as e:
                logging.exception(e)
                self._exception_queue.put(e)
                self.stop = True
        if self.stop and self._websocket is not None and self._websocket.open:
            logging.debug("Closing websocket.")
            await self._websocket.close()
        logging.debug("Communication with server ended.")
        return

    async def _connect_to_server(self):
        """Creates a websocket connection using the uri of this websocket thread.

        Raises: - OSError when something went wrong, e.g. too many attempts to connect to the websocket host occurred.
                - InvalidURI - passed on from `websockets.client.connect` if `self._websocket_uri` is invalid.
                - InvalidHandshake - passed on from `websockets.client.connect` if the opening handshake fails.
        """
        self._websocket = await websockets.client.connect(self._websocket_uri, extra_headers=self._headers,
                                                          ping_interval=None)
