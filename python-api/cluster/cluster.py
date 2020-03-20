class Connector(object):
    """Allows communication with Cluster API server.

    This Connector class allows communication with the Cluster API server by returning tasks
    from the server whenever any are available and by replying with a response.
    """

    def __init__(self):
        self._tasks = list()  # store received tasks
        self._server_timeout = 4  # timeout used while checking for server messages

    def has_task(self):
        """Checks whether the server has any tasks available.

        Sends a message to the server to check whether it has any tasks that should be processed.

        Returns:
            True if and only if there is a task to be processed.
        """
        """
        CONCRETELY: We use this function to actively wait on a request, but if you
        could implement a "wait-and-wake-up" method where the execution of the script
        just waits until there is a request and then wakes up that would be even better
        since active waiting will create unnecessary network traffic :)
        """

    def get_next_task(self):
        """
        Waits for the next task from the server and returns it.

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
             A task to be processed as a JSON object.
        """

        """
        1st iteration:
            Send a simple HTTP request to API server requesting task to be performed.
            Append received tasks to _tasks and return first item of list if not empty (shouldn`t be 
            possible, because this method only ends when a task has been received and appended to _tasks).
        
        2nd iteration:
            Connect to server using web socket, so a permanent connection is made. This way the server
            can push directly any tasks without this client having to poll every now and then.
        """

    def reply(self, response):
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
