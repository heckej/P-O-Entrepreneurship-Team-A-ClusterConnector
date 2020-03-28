using ClusterConnector.Models.NLP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace ClusterAPI.Controllers.NLP.Requests
{
    public class QuestionOffenseController : ApiController
    {
        private static readonly String DEFAULT_ACTION = "estimate_offensiveness";

        [Route("api/NLP/questionOffensive")]
        public IHttpActionResult GetTestQuestion()
        {
            NLPActionOffenseRating[] actions = new NLPActionOffenseRating[1];
            NLPActionOffenseRating nLPAction = new NLPActionOffenseRating();
            nLPAction.Action = DEFAULT_ACTION;
            nLPAction.Question_id = -1;
            nLPAction.Question = "ROSES ARE RED, VIOLETS ARE BLUE, GANDALF IS A WIZARD, NOW FLY YOU FOOL!";
            nLPAction.Msg_id = -1;

            if (nLPAction == null)
            {
                return NotFound();
            }
            actions[0] = nLPAction;
            return Ok(actions);
        }
    }
}
