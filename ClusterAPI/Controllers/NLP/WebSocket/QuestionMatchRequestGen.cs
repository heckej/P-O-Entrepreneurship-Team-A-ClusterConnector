using ClusterAPI.Utilities.WebSockets;
using ClusterConnector.Models.NLP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Web.Http;

namespace ClusterAPI.Controllers.NLP
{
    public class QuestionMatchRequestGen : IRequestGen
    {
        private static readonly String DEFAULT_ACTION_MATCH = "match_questions";

        public string GenerateRequest(params object[] args)
        {
            NLPActionQuestionMatch[] actions = new NLPActionQuestionMatch[1];
            NLPActionQuestionMatch nLPAction = new NLPActionQuestionMatch();
            nLPAction.Action = DEFAULT_ACTION_MATCH;
            nLPAction.Question_id = -1;
            nLPAction.Question = "ROSES ARE RED, VIOLETS ARE BLUE, GANDALF IS A WIZARD, NOW FLY YOU FOOL!";
            nLPAction.Compare_questions = new List<ClusterConnector.Models.NLP.NLPQuestion>() { new NLPQuestion() { Question = "TEST QUESTION1", Question_id = -1 } };
            nLPAction.Msg_id = -1;
            actions[0] = nLPAction;

            return JsonSerializer.Serialize(actions);
        }
    }
}
