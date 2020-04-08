using ClusterLogic.Models;
using System;
using System.Web.Http;

namespace ClusterAPI.Controllers.NLP.Requests
{
    public class QuestionOffenseController : ApiController
    {
        private static readonly String DEFAULT_ACTION = "estimate_offensiveness";

        [Route("api/NLP/questionOffensive")]
        public IHttpActionResult GetTestQuestion()
        {
            OffensivenessModelRequest[] actions = new OffensivenessModelRequest[1];
            OffensivenessModelRequest nLPAction = new OffensivenessModelRequest();
            nLPAction.action = DEFAULT_ACTION;
            nLPAction.question_id = -1;
            nLPAction.question = "ROSES ARE RED, VIOLETS ARE BLUE, GANDALF IS A WIZARD, NOW FLY YOU FOOL!";
            nLPAction.msg_id = -1;

            if (nLPAction == null)
            {
                return NotFound();
            }
            actions[0] = nLPAction;
            return Ok(actions);
        }
    }
}
