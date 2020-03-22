import pytest
from cluster_connector.cluster import cluster
api_url = "http://localhost/api/nlp"
task1 = {"action":"match_questions","question_id":123,"question":"What is your name?","compare_questions":{"0":{"question_id":111,"question":"Who are you?"},"1":{"question_id":222,"question":"Who am I?"},"2":{"question_id":333},"question":"What is my name?"},"msg_id":1234567890}
task2 = {"action":"match_questions","question_id":567,"question":"How are you?","compare_questions":{"0":{"question_id":111,"question":"Who are you?"},"1":{"question_id":444,"question":"What are you doing?"},"2":{"question_id":555},"question":"How are you doing?"},"msg_id":1234567892}
task3 = {"action":"estimate_offensiveness","question_id":100,"question":"You are a pizza.","msg_id":1234567891}
tasks = [task1, task2, task3]
tasks_in_progress = {task1['msg_id']: task1, task2['msg_id']: task2, task3['msg_id']: task3}
reply1 = {"question_id": 123, "possible_matches": [{"question_id": 111, "prob": 0.789},
                                                   {"question_id": 333, "prob": 0.654}], "msg_id": 1234567890}
expected1 = {"msg": "Matched question saved.", "question_id": reply1['question_id']}
reply2 = {"question_id": 100, "prob": 0.123, "msg_id": 1234567890}
expected2 = {"msg": "Offensiveness value saved.", "value": reply2['prob']}
reply3 = {"question_id": 123, "possible_matches": [{"question_id": 111, "prob": 0.789},
                                                   {"question_id": 333, "prob": 0.654}], "msg_id": 1234567892}
expected3 = expected1
replies = [reply1, reply2, reply3]
expected_responses = [expected1, expected2, expected3]
indices = range(0, 1)


def test_connector_get_next_task_prefetch():
    con = cluster.Connector()
    con._base_request_uri = api_url
    con.prefetch = True
    assert con.get_next_task() == task1
    assert con._tasks_in_progress == {task1['msg_id']: task1}
    assert con._tasks == [task2, task3]
    assert con.get_next_task() == task2
    assert con._tasks_in_progress == {task1['msg_id']: task1, task2['msg_id']: task2}
    assert con._tasks == [task3]
    assert con.get_next_task() == task3
    assert con._tasks_in_progress == tasks_in_progress


def test_connector_get_next_task_no_prefetch():
    con = cluster.Connector()
    con._base_request_uri = api_url
    con.prefetch = False
    assert con.get_next_task() == task1
    assert len(con._tasks) == 0
    assert con._tasks_in_progress == {task1['msg_id']: task1}
    assert con.get_next_task() == task2
    assert len(con._tasks) == 0
    assert con._tasks_in_progress == {task1['msg_id']: task1, task2['msg_id']: task2}
    assert con.get_next_task() == task3
    assert len(con._tasks) == 0
    assert con._tasks_in_progress == tasks_in_progress


@pytest.mark.parametrize("i", indices)
def test_connector_reply(i):
    con = cluster.Connector()
    con._base_request_uri = api_url
    con.get_next_task()
    assert con.reply(replies[i]) == expected_responses[i]
    assert replies[i]['msg_id'] not in con._tasks_in_progress.keys()
