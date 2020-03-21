import time
import requests
import json


class Connector(object):
    """Allows communication with Cluster API server.

    This Connector class allows communication with the Cluster API server by returning tasks
    from the server whenever any are available and by replying with a response.

    Attributes:
        prefetch: A boolean that enables this Connector to fetch all available tasks. If `prefetch` is set to False,
            only one task will be fetched at a time. To improve performance you may want to leave this set to True,
            because that way less data transfers may be needed, though there's no guaranty for that.
    """

    _base_request_uri = "https://clusterapi20200320113808.azurewebsites.net/api/nlp"

    def __init__(self):
        self.prefetch = True
        self._tasks = list()  # store non processed received tasks
        self._tasks_in_progress = dict()  # keep track of work in progress
        self._server_timeout = 4  # timeout used while checking for server messages

    def has_task(self) -> bool:
        """Checks whether the server has any tasks available.

        Asks the server to check whether it has any tasks that should be processed.
        This method should only be used when there is no reason to use `get_next_task()` afterwards. Because that kind
        of situations seems to be quite uncommon, this method will likely be removed in one of the next releases.

        Returns:
            True if and only if there is a task to be processed.
        """
        return True

    def get_next_task(self, timeout=None) -> any:
        """
        Waits for the next task from the server and returns it as a dictionary.

        Waits until the server has delivered a task or until timeout if a timeout is set.

        Args:
            timeout: The number of seconds to wait before returning without result. In case the timeout is set to None,
                then the method will only return upon receiving a task from the server.

        Currently two possible JSON structures can be expected:

        1. The server asks to match a question with an undefined number of questions:

                {
                    "action": "match_questions",
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
                    "action": "estimate_offensiveness",
                    "question_id": 100,
                    "question": "XXX",
                    "msg_id": 1234567890
                 }

        Returns:
             A task to be processed as a JSON object or None when no task was received before timeout.
        """
        """
        TODO(Joren) 1st iteration:
            Send a simple HTTP request to API server requesting task to be performed.
            Append received tasks to _tasks and return first item of list if not empty (shouldn`t be 
            possible, because this method only ends when a task has been received and appended to _tasks).
        
        TODO(Joren) 1st-2nd iteration: 
            Return first element of _tasks and update _tasks in background without causing delay in case _tasks is not
            empty.
        
        TODO(Joren) 2nd-3rd iteration:
            Connect to server using web socket, so a permanent connection is made. This way the server
            can push directly any tasks without this client having to poll every now and then.
        """

        tasks_found = False
        if len(self._tasks) == 0:
            # no tasks left, ask the server
            if timeout is not None and timeout > 0:
                # equally divide the given timeout
                timeout_offensive = timeout // 2
                timeout_unmatched = timeout - timeout_offensive

                start_time = time.time()
                end_time = start_time
                while end_time - start_time < timeout and not tasks_found:
                    path_unmatched = self._base_request_uri + "/questions/unmatched"
                    tasks_found = self._request_questions(path_unmatched, timeout)

                    # request questions of which the offensiveness has to be tested
                    path_offensive = self._base_request_uri + "/questions/offensive/undefined"
                    tasks_found = tasks_found | self._request_questions(path_offensive, timeout)

                    # update the timer
                    end_time = time.time()

        if not tasks_found:
            return None
        task = self._tasks[0]
        self._tasks.remove(0)
        self._tasks_in_progress[task['msg_id']] = task

        return task

    def _request_questions(self, path, timeout):
        request_uri = self._base_request_uri + path
        request = requests.get(request_uri, timeout=timeout)
        if request.status_code == 200:
            # Status == OK
            questions = json.loads(request.json())
            self._tasks.append(questions)
            return True
        return False

    def reply(self, response: dict) -> None:
        """Sends the given response to the server.

        Args:
            response: A JSON object.

            As a response argument, currently two possible structures can be parsed:

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
                        ]
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
        """
