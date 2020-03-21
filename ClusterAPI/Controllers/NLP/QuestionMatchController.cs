using ClusterConnector.Models.NLP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace ClusterAPI.Controllers.NLP
{
    public class QuestionMatchController : ApiController
    {
        private static String DEFAULT_ACTION = "Answers.MATCH_QUESTIONS";

        public IHttpActionResult GetTestQuestion()
        {
            NLPAction nLPAction = new NLPAction();
            nLPAction.Action = DEFAULT_ACTION;
            nLPAction.Question_id = -1;
            nLPAction.Question = "ROSES ARE RED, VIOLETS ARE BLUE, GANDALF IS A WIZARD, NOW FLY YOU FOOL!";
            nLPAction.Compare_questions = new List<ClusterConnector.Models.NLP.NLPQuestion>() { new NLPQuestion() { Question = "TEST QUESTION1" , Question_id = -1 } };
            nLPAction.Msg_id = -1;

            if (nLPAction == null)
            {
                return NotFound();
            }
            var temp = Ok(nLPAction);
            return Ok(nLPAction);
        }
    }
}
