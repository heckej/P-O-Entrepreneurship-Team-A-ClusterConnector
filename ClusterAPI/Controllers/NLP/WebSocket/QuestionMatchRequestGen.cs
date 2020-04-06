using ClusterAPI.Utilities.WebSockets;
using ClusterLogic.Models;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace ClusterAPI.Controllers.NLP
{
    public class QuestionMatchRequestGen : IRequestGen
    {
        private static readonly String DEFAULT_ACTION_MATCH = "match_questions";

        public string GenerateRequest(params object[] args)
        {
            MatchQuestionModelRequest[] actions = new MatchQuestionModelRequest[1];
            MatchQuestionModelRequest nLPAction = new MatchQuestionModelRequest();
            nLPAction.action = DEFAULT_ACTION_MATCH;
            nLPAction.question_id = -1;
            nLPAction.question = "ROSES ARE RED, VIOLETS ARE BLUE, GANDALF IS A WIZARD, NOW FLY YOU FOOL!";
            nLPAction.compare_questions = new List<NLPQuestionModelInfo>() { new NLPQuestionModelInfo() { question = "TEST QUESTION1", question_id = -1 } }.ToArray();
            nLPAction.msg_id = -1;
            actions[0] = nLPAction;

            return JsonSerializer.Serialize(actions);
        }
    }
}
