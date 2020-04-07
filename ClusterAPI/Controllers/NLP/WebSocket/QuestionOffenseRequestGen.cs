using ClusterAPI.Utilities.WebSockets;
using ClusterLogic.Models;
using ClusterLogic.NLPHandler;
using System;
using System.Text.Json;

namespace ClusterAPI.Controllers.NLP
{
    public class QuestionOffenseRequestGen : IRequestGen
    {
        private static readonly String DEFAULT_ACTION_OFFENSE = "estimate_offensiveness";

        public string GenerateRequest(params object[] args)
        {
            OffensivenessModelRequest[] actions_new = ProcessNLPRequest.ProcessNLPQuestionOffenseRequest(args);

            OffensivenessModelRequest[] actions = new OffensivenessModelRequest[1];
            OffensivenessModelRequest nLPAction = new OffensivenessModelRequest();
            nLPAction.action = DEFAULT_ACTION_OFFENSE;
            nLPAction.question_id = -1;
            nLPAction.question = "ROSES ARE RED, VIOLETS ARE BLUE, GANDALF IS A WIZARD, NOW FLY YOU FOOL!";
            nLPAction.msg_id = -1;
            actions[0] = nLPAction;

            return JsonSerializer.Serialize(actions);
        }
    }
}
