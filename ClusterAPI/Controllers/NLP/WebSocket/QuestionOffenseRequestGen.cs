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
    public class QuestionOffenseRequestGen : IRequestGen
    {
        private static readonly String DEFAULT_ACTION_OFFENSE = "estimate_offensiveness";

        public string GenerateRequest(params object[] args)
        {
            NLPActionOffenseRating[] actions = new NLPActionOffenseRating[1];
            NLPActionOffenseRating nLPAction = new NLPActionOffenseRating();
            nLPAction.Action = DEFAULT_ACTION_OFFENSE;
            nLPAction.Question_id = -1;
            nLPAction.Question = "ROSES ARE RED, VIOLETS ARE BLUE, GANDALF IS A WIZARD, NOW FLY YOU FOOL!";
            nLPAction.Msg_id = -1;
            actions[0] = nLPAction;

            return JsonSerializer.Serialize(actions);
        }
    }
}
