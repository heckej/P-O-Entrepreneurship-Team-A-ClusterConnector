import time
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

    .. versionadded::0.1.0
    .. versionchanged::1.0.0

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

    IS_NONSENSE = "is_nonsense"
    """Find out if a string contains nonsense
    
    .. versionadded::1.0.0
    """

    @classmethod
    def has_value(cls, value):
        return value in cls._value2member_map_


class Connector(object):
    """Allows communication with Cluster API server.

    This Connector class allows communication with the Cluster API server by returning NLP tasks
    from the server whenever any are available and by replying with a response.

    .. versionadded::0.1.0
    .. versionchanged::0.2.0
    .. versionchanged::1.0.0

    Raises:
        Exception: Something went wrong while trying to communicate with the server. The range of these exceptions is
            mostly focused on `OSError` and `websockets.exceptions.InvalidMessage`, but is not limited to those.

    Debugging:
        To enable logging of debugging messages, use the following statements:
        ```
            >> import logging, sys
            >> logging.basicConfig(stream=sys.stderr, level=logging.DEBUG)
        ```
    """

    necessary_task_keys = {"msg_id", "action"}
    """Set of keys that have to be in a task dictionary to be a valid task.
    
    .. versionadded::0.2.0
    """

    _generic_actions = {Actions.ESTIMATE_OFFENSIVENESS, Actions.IS_NONSENSE}
    """Set of actions that can be applied on both questions and answers.
    
    .. versionadded::1.0.0
    """

    __version__ = '1.0.0'

    def __init__(self, websocket_uri="wss://clusterapi20200320113808.azurewebsites.net/api/NLP/WS",
                 websocket_connection_timeout=10):
        """
        Args:
            websocket_uri: A custom uri referencing the websocket host that should be used.

            websocket_connection_timeout: The timeout to be set for the websocket connection before giving up. By
                default set to 10 seconds.
        """
        self._tasks = list()  # store non processed received tasks
        self._tasks_in_progress = dict()  # keep track of work in progress

        self._websocket_connection_timeout = websocket_connection_timeout
        self._websocket_uri = websocket_uri
        self._reply_queue = collections.deque()  # keep list of replies to send
        self._websocket_thread = None
        self._websocket_exceptions = queue.Queue()  # queue to keep exceptions thrown by websocket thread
        self._init_websocket_thread()

    def reset_connection(self):
        """Resets the websocket thread.

        .. versionadded::0.2.0
        """
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

        .. versionadded::0.1.0
        .. versionchanged::0.2.0
        .. versionchanged::1.0.0

        Checks whether the web socket connection is still alive and whether any tasks are available in the cache.

        Returns:
            True if and only if there is a task to be processed.

        Raises:
            Exception: The websocket thread has passed an exception. The passed exception is raised by this method.
        """
        self._checkout_websocket()
        return len(self._tasks) > 0

    def get_next_task(self, timeout: float = None) -> any:
        """
        Waits for the next task from the server and returns it as a dictionary.

        Waits until the server has delivered a task or until timeout if a timeout is set.

        .. versionadded::0.1.0
        .. versionchanged::0.2.0
        .. versionchanged::1.0.0

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
                    "sentence_id": 100,
                    "sentence": "XXX",
                    "msg_id": 1234567890
                 }

        3. The server asks to check if a sentence is nonsense or not:

                {
                    "action": Actions.IS_NONSENSE,
                    "sentence_id": 100,
                    "sentence": "XXX",
                    "msg_id": 1234567890
                }

        Note that other keys can be present, but the keys mentioned in the example will be part of the actual result.

        Returns:
             A task to be processed as a JSON object or None when no task was received before timeout.

        Raises:
            Exception: The websocket thread has passed an exception. The passed exception is raised by this method.
        """
        logging.debug("Get task using websocket")
        tasks_found = len(self._tasks) > 0
        start_time = time.time()
        time_passed = 0
        while not tasks_found and (timeout is None or (time_passed < timeout)):
            self._checkout_websocket()
            tasks_found = len(self._tasks) > 0
            time_passed = time.time() - start_time  # keep track of the passed time
        if tasks_found:
            # Remove task from task list and add it to the tasks in progress list.
            task = self._tasks.pop(0)
            self._tasks_in_progress[task['msg_id']] = task
        else:
            task = None
        return task

    def close(self):
        """Sends a stop signal to the thread running the websocket connection of this connector.

        .. versionadded::0.2.0
        """
        self._websocket_thread.stop = True

    @classmethod
    def is_valid_task(cls, task: dict):
        """Returns True if and only if the given dictionary contains the keys that are in the `cls.necessary_task_keys`
        set.

        .. versionadded::0.2.0
        """
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
        try:
            response = json.loads(response)
        except json.decoder.JSONDecodeError as e:
            response = ""
            logging.debug(e)
        if type(response) == list:
            for task in response:
                task = cls._parse_response_dict(task)
                parsed_response.append(task)
        elif type(response) == dict:
            task = cls._parse_response_dict(response)
            parsed_response.append(task)
        return parsed_response

    @classmethod
    def _parse_response_dict(cls, response_dict: dict) -> dict:
        """Converts keys of given dictionary and dictionaries in a list in the given dictionary to lower case.
        Also adds sentence and sentence_id keys to replace question/answer and question_id/answer_id.
        """
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
        if parsed_response["action"] in cls._generic_actions:
            # add generic keys sentence en sentence_id instead of answer/question
            if "question" in parsed_response.keys():
                parsed_response["sentence"] = parsed_response["question"]
                parsed_response["sentence_id"] = parsed_response["question_id"]
            elif "answer" in parsed_response.keys():
                parsed_response["sentence"] = parsed_response["answer"]
                parsed_response["sentence_id"] = parsed_response["answer_id"]
        return parsed_response

    def _parse_request(self, request: dict) -> dict:
        """Processes a dictionary received from the NLP and returns a dictionary that complies to
        structure that can be understood by the server.

        Args:
            request: The request from the NLP as a dictionary.

        Returns:
            A dictionary that complies to the structure understood by the server containing the
            information of the given `request` as far as the structure allows it.
        """
        parsed_request = request
        # return from generic sentence(_id) to question/answer(_id)
        original_response = self._tasks_in_progress[request["msg_id"]]
        if "question" in original_response.keys():
            parsed_request["question"] = request["sentence"]
            parsed_request["question_id"] = request["sentence_id"]
        elif "answer" in original_response.keys():
            parsed_request["answer"] = request["sentence"]
            parsed_request["answer_id"] = request["sentence_id"]
        return parsed_request

    def reply(self, response: dict):
        """Sends the given response to the server.

        .. versionadded::0.1.0
        .. versionchanged::1.0.0

        Checks whether the websocket connection is still alive and delivers the given `response` to the websocket
        thread.

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
                        "sentence_id": 100,
                        "prob": 0.123,
                        "msg_id": 1234567890
                    }

            3. A reply to an `is_nonsense`:

                    {
                        "sentence_id": 100,
                        "nonsense": True,
                        "msg_id": 1234567890
                    }

            The `msg_id` is always used to include in the reply so that the server knows to
            which task the reply belongs. It corresponds to the `msg_id` from a task from
            the `get_next_task()` method.

        Raises:
            Exception: Something went wrong while sending the reply to the server.
                This exception may become more specific in a future release, but for now it is kept as general as
                possible, so any implementation changes don't effect these specifications.
        """
        self._checkout_websocket()
        action = self._tasks_in_progress[response['msg_id']]['action'].lower()
        if Actions.has_value(action) and response['msg_id'] in self._tasks_in_progress.keys():
            data = self._parse_request(response)
            del self._tasks_in_progress[response['msg_id']]
            self._reply_queue.append(json.dumps(data))
