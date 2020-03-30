import time
import requests
import threading
import json
import queue
import collections
from . import websocket_thread
import asyncio
import logging
# import sys
from enum import Enum
# logging.basicConfig(stream=sys.stderr, level=logging.DEBUG)


class Actions(Enum):
    """Enumeration of recognized actions.

    The actions that are recognized by the connector and therefore can be returned are enumerated in this class.
    To loop through all of the actions in this enumeration, simply use

        for action in Actions:
               # do something with action
    """

    MATCH_QUESTIONS = "match_questions"
    """Match questions."""

    ESTIMATE_OFFENSIVENESS = "estimate_offensiveness"
    """Estimate the offensiveness of a question."""

    NO_WORK = "no_work"
    """There server has no tasks to process."""

    @classmethod
    def has_value(cls, value):
        return value in cls._value2member_map_


class Connector(object):
    """Allows communication with Cluster API server.

    This Connector class allows communication with the Cluster API server by returning NLP tasks
    from the server whenever any are available and by replying with a response.

    Attributes:
        prefetch: A boolean that enables this Connector to fetch all available tasks. If `prefetch` is set to False,
            only one task will be fetched at a time. To improve performance you may want to leave this set to True,
            because that way less data transfers may be needed, though there's no guaranty for that.

        fetch_in_background: A boolean that enables this Connector to fetch tasks in the background and return the
            the next task immediately when available. If `fetch_in_background` is set to False, new tasks may take
            additional time to fetch when no tasks are available immediately. To improve performance you may want to
            leave this set to True, because that way tasks may be fetched before they are needed, so no additional time
            is required when requesting the next task using `get_next_task()`.
    """

    necessary_task_keys = {"msg_id", "action"}
    """Set of keys that have to be in a task dictionary to be a valid task."""

    def __init__(self, use_websocket: bool = False,
                 websocket_uri="wss://https://clusterapi20200320113808.azurewebsites.net/api/NLP/WS",
                 websocket_connection_timeout=10):
        """
        Args:
            use_websocket: A boolean that enables usage of websockets. See `use_websocket` under Attributes.

            websocket_uri: A custom uri referencing the websocket host that should be used.

            websocket_connection_timeout: The timeout to be set for the websocket connection before giving up. By
                default set to 10 seconds.

        Raises:
            Exception: Something went wrong while communicating with the server.
                This exception may become more specific in a future release, but for now it is kept as general as
                possible, so any implementation changes don't effect these specifications.
        """
        self.prefetch = True
        self._tasks = list()  # store non processed received tasks
        self._tasks_in_progress = dict()  # keep track of work in progress

        self._server_timeout = 4  # timeout used while checking for server messages
        self._base_request_uri = "https://clusterapi20200320113808.azurewebsites.net/api/NLP"
        self._time_until_retry = 2  # the time to sleep between two attempts to connect to the server
        self._request_paths = {'offensive': '/QuestionOffensive', 'unmatched': '/QuestionMatch'}
        self._post_paths = {'offensive': '/QuestionOffensivesness', 'matched': '/QuestionsMatch'}
        self._session = requests.Session()  # start session to make use of HTTP keep-alive functionality
        self._session.headers.update({'Accept': 'application/json'})  # make sure to request json only

        self._request_thread = None
        self.fetch_in_background = True
        self.use_websocket = use_websocket
        self._websocket_connection_timeout = websocket_connection_timeout
        if use_websocket:
            logging.warning("The use of websockets is currently still under development. Make sure to close the "
                            "connection when done using `close()`. To print debugging statements, "
                            "use\n>> import logging, sys\n>> logging.basicConfig(stream=sys.stderr, "
                            "level=logging.DEBUG)")
        else:
            logging.warning("In the next release websockets will be enabled by default.")
        self._websocket_uri = websocket_uri
        self._reply_queue = collections.deque()  # keep list of replies to send
        self._websocket_thread = None
        self._websocket_exceptions = queue.Queue()  # queue to keep exceptions thrown by websocket thread
        if self.use_websocket:
            self._init_websocket_thread()

    def reset_connection(self):
        """Resets the websocket thread."""
        self._init_websocket_thread()

    def _init_websocket_thread(self):
        """Initialize a new thread running a websocket connection.

        Post:
            In case a websocket thread had been assigned before, the previous websocket thread is stopped and a new
            websocket thread is started.
            `self._websocket_thread` equals the newly assigned websocket thread.
        """
        if self._websocket_thread is not None:
            self._websocket_thread.stop = True
        # Clear exceptions in case any are still in the queue
        logging.debug("Clearing exception queue.")
        with self._websocket_exceptions.mutex:
            self._websocket_exceptions.queue.clear()
        # Let asynchronous websocket run in separate thread, so it doesn't block
        logging.debug("Starting new thread.")
        self._websocket_thread = websocket_thread.WebsocketThread(self._websocket_uri, self._websocket_exceptions,
                                                                  self._add_tasks,
                                                                  self._reply_queue, asyncio.get_event_loop(),
                                                                  self._websocket_connection_timeout)
        self._websocket_thread.start()
        logging.debug("Thread " + self._websocket_thread.getName() + " started.")

    def _checkout_websocket(self):
        """Checks whether the websocket thread is still alive and whether it has passed exceptions.

        Raises:
            Exception: The websocket thread has passed an exception. The passed exception is raised by this method.
        """
        # check if websocket still alive and hasn't thrown any exceptions
        if not self._websocket_exceptions.empty():
            # Websocket thread passed an exception.
            exception = self._websocket_exceptions.get()
            self._websocket_thread.stop = True
            logging.debug("An exception occurred in the websocket thread.")
            raise exception
        elif self._websocket_thread is None or not self._websocket_thread.is_alive():
            logging.debug("Reinitializing websocket thread.")
            self._init_websocket_thread()

    def _add_tasks(self, message):
        """Parses a given response and adds tasks from message to the queue if needed."""
        received_tasks = Connector._parse_response(message)
        for task in received_tasks:
            self._add_task(task)

    def _add_task(self, task):
        """Adds the given task to the task queue if it is valid and not yet in the task or tasks in progress queue."""
        if not Connector.is_valid_task(task):
            logging.debug("Task with invalid structure received: " + str(task))
        elif task not in self._tasks and task['msg_id'] not in self._tasks_in_progress:
            # only add task if valid and not in the (progress) task list already
            self._tasks.append(task)
            logging.debug("Task added: " + str(task))
        else:
            # task already received
            logging.debug("Message id " + str(task['msg_id']) + " already in task or tasks in progress queue.")

    def has_task(self) -> bool:
        """Checks whether the server has any tasks available.

        Asks the server to check whether it has any tasks that should be processed.
        This method should only be used when there is no reason to use `get_next_task()` afterwards. Because that kind
        of situations seems to be quite uncommon, this method will likely be removed in one of the next releases.

        Returns:
            True if and only if there is a task to be processed.

        Raises:
            Exception: The websocket thread has passed an exception. The passed exception is raised by this method.
        """
        if self.use_websocket:
            self._checkout_websocket()
        uri_unmatched = self._request_paths['unmatched']
        uri_offensive = self._request_paths['offensive']
        # print("using websocket:", self.use_websocket)
        if self.use_websocket:
            return len(self._tasks) > 0
        else:
            return self._request_tasks(uri_unmatched, 0.25, False) or self._request_tasks(uri_offensive, 0.25, False)

    def get_next_task(self, timeout: float = None) -> any:
        """
        Waits for the next task from the server and returns it as a dictionary.

        Waits until the server has delivered a task or until timeout if a timeout is set.

        Args:
            timeout: The number of seconds to wait before returning without result. In case the timeout is set to None,
                then the method will only return upon receiving a task from the server.

        Currently two possible JSON structures can be expected:

        1. The server asks to match a question with an undefined number of questions:

                {
                    "action": Actions.MATCH_QUESTIONS,
                    "question_id": 123,
                    "question": "XXX",
                    "compare_questions": [
                        {
                            "question_id": 111,
                            "question": "AAA"
                        },
                        {
                            "question_id": 222,
                            "question": "BBB"
                        },
                        {
                            "question_id": 333,
                            "question": "CCC"
                        },
                    ],
                    "msg_id": 1234567890
                }


        2. The server asks to estimate the offensiveness of a sentence:

                 {
                    "action": Actions.ESTIMATE_OFFENSIVENESS,
                    "question_id": 100,
                    "question": "XXX",
                    "msg_id": 1234567890
                 }

        Note that other keys can be present, but the keys mentioned in the example will be part of the actual result.

        Returns:
             A task to be processed as a JSON object or None when no task was received before timeout.

        Raises:
            Exception: something went wrong while communicating with the server.
                This exception may become more specific in a future release, but for now it is kept as general as
                possible, so any implementation changes don't effect these specifications.
        """
        """
        TODO(Joren) 1st iteration:
            Send a simple HTTP request to API server requesting task to be performed.
            Append received tasks to _tasks and return first item of list if not empty (shouldn`t be 
            possible, because this method only ends when a task has been received and appended to _tasks).

    def close(self):
        """Sends a stop signal to the thread running the websocket connection of this connector."""
        self._websocket_thread.stop = True

        TODO(Joren) 2nd-3rd iteration:
            Connect to server using web socket, so a permanent connection is made. This way the server
            can push directly any tasks without this client having to poll every now and then.
        """

        tasks_found = False
        path_unmatched = self._request_paths['unmatched']
        path_offensive = self._request_paths['offensive']
        if len(self._tasks) == 0:
            # no tasks left, ask the server
            if timeout is None or timeout > 0:
                time_passed = 0
                start_time = time.time()
                sleep = False
                sleep_start = 0
                while not tasks_found and (timeout is None or (time_passed < timeout)):
                    if not sleep and (self._request_thread is None or not self._request_thread.is_alive()):
                        # only try when not sleeping and when no tasks are being requested in a background already
                        if timeout is not None:
                            time_left = timeout - time_passed
                            # equally divide the given timeout
                            timeout_offensive = time_left / 2
                            timeout_unmatched = time_left - timeout_offensive
                        else:
                            timeout_unmatched = None
                            timeout_offensive = None
                        tasks_found = self._request_tasks(path_unmatched, timeout_unmatched)

                        if self.prefetch or not tasks_found:
                            # request questions of which the offensiveness has to be tested
                            # if prefetching disabled and already task found, then don't look for another task
                            tasks_found = tasks_found | self._request_tasks(path_offensive, timeout_offensive)
                        sleep_start = time.time()  # start sleeping
                    time_passed = start_time - time.time()  # keep track of the passed time
                    # stay asleep until 'time_until_retry' seconds passed and
                    # there is more time left than 'time_until_retry' seconds
                    sleep = (time.time() - sleep_start < self._time_until_retry) and \
                            (timeout is None or self._time_until_retry < timeout - time_passed)

            if not tasks_found:
                return None
        else:
            # still tasks left, but there might be new ones to be fetched
            if self.fetch_in_background and self._request_thread is None or not self._request_thread.is_alive():
                self._request_thread = threading.Thread(target=self._request_tasks_from_paths,
                                                        args=([path_unmatched, path_offensive], self._server_timeout))
                self._request_thread.start()
        task = self._tasks.pop(0)
        self._tasks_in_progress[task['msg_id']] = task

        return task

    def _request_tasks_from_paths(self, paths: list, timeout: float, append: bool = True):
        """Requests tasks from the server at all given paths."""
        for path in paths:
            self._request_tasks(path, timeout, append)

    def _request_tasks(self, path: str, timeout: float, append: bool = True):
        """Sends a request to the server to receive tasks."""
        request_uri = self._base_request_uri + path
        request = self._session.get(request_uri, timeout=timeout)
        if request.status_code == 200:
            # Status == OK
            # JSON response can be in different format than the one that should be returned
            received_tasks = self._parse_response(request.json())
            # The server might not have tasks.
            if len(received_tasks) == 0 or received_tasks[0]['action'].lower() == Actions.NO_WORK.value:
                return False
            if self.prefetch:
                # fetch all available tasks
                new_task_found = False
                for task in received_tasks:
                    if task not in self._tasks and task['msg_id'] not in self._tasks_in_progress and append:
                        # only add task if not in the (processing) task list already and appending is enabled
                        self._tasks.append(task)
                        new_task_found = True
                return new_task_found
            else:
                # prefetching disabled, so only fetch one question that is not in the task list already, but not if
                # appending is disabled
                for task in received_tasks:
                    if task not in self._tasks and task['msg_id'] not in self._tasks_in_progress and append:
                        self._tasks.append(task)
                        return True
                # all available tasks have been fetched before
                return False
        return False

    @classmethod
    def is_valid_task(cls, task: dict):
        """Returns True if and only if the given dictionary contains the keys that are in the `cls.necessary_task_keys`
        set."""
        return set(task.keys()).intersection(cls.necessary_task_keys) == cls.necessary_task_keys

    @classmethod
    def _parse_response(cls, response) -> list:
        """Processes a dictionary or a list of dictionaries received from the server and returns a list of dictionaries
         that comply to the structure of the result of `get_next_task()`.

        Args:
            response: The response from the server as a dictionary or a list of dictionaries.

        Returns:
            A list of dictionaries that comply to the structure of the result of `get_next_task()` containing the
            information of the given `response` as far as the structure allows it.
        """
        parsed_response = list()
        if type(response) == list:
            for task in response:
                task = Connector._parse_response_dict(task)
                parsed_response.append(task)
        elif type(response) == dict():
            task = Connector._parse_response_dict(response)
            parsed_response.append(task)
        return parsed_response

    @classmethod
    def _parse_response_dict(cls, response_dict: dict) -> dict:
        parsed_response = dict()
        for key, value in response_dict.items():
            if type(value) == list:
                new_value = list()
                for item in value:
                    if type(item) == dict:
                        item = {k.lower(): v for k, v in item.items()}  # deepest expected nesting is this level
                    new_value.append(item)
                value = new_value
            key = key.lower()
            parsed_response[key] = value
        return parsed_response

    @classmethod
    def _parse_request(cls, request: dict) -> dict:
        """Processes a dictionary received from the NLP and returns a dictionary that complies to
        structure that can be understood by the server.

        Args:
            request: The request from the NLP as a dictionary.

        Returns:
            A dictionary that complies to the structure understood by the server containing the
            information of the given `request` as far as the structure allows it.
        """
        return request

    def reply(self, response: dict) -> dict:
        """Sends the given response to the server.

        Args:
            response: A dictionary built like a JSON object.

            The effect of replying with a response that doesn't follow one of the below mentioned structures
            is undefined. As a response argument, currently two possible structures are allowed:

            1. A reply to a `match_question` containing a top x of comparable questions:

                    {
                        "question_id": 123,
                        "possible_matches": [
                            {
                                "question_id": 111,
                                "prob": 0.789
                            },
                            {
                                "question_id": 333,
                                "prob": 0.654
                            }
                        ],
                        "msg_id": 1234567890
                    }

            2. A reply to an `estimate_offensiveness`:

                    {
                        "question_id": 100,
                        "prob": 0.123,
                        "msg_id": 1234567890
                    }

            The `msg_id` is always used to include in the reply so that the server knows to
            which task the reply belongs. It corresponds to the `msg_id` from a task from
            the `get_next_task()` method.

        Raises:
            Exception: something went wrong while sending the reply to the server.
                This exception may become more specific in a future release, but for now it is kept as general as
                possible, so any implementation changes don't effect these specifications.
        """

        action = self._tasks_in_progress[response['msg_id']]['action'].lower()
        if Actions.has_value(action) and response['msg_id'] in self._tasks_in_progress.keys():
            request_uri = self._base_request_uri
            if action == Actions.MATCH_QUESTIONS.value:
                request_uri += self._post_paths['matched']
            elif action == Actions.ESTIMATE_OFFENSIVENESS.value:
                request_uri += self._post_paths['offensive']
            del self._tasks_in_progress[response['msg_id']]
            data = self._parse_request(response)
            request = self._session.post(request_uri, json=data)
            return request.json()
