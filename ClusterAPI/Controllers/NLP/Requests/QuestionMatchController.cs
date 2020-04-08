using ClusterLogic.Models;
using System;
using System.Collections.Generic;
using System.Web.Http;

namespace ClusterAPI.Controllers.NLP
{
    public class QuestionMatchController : ApiController
    {
        private static readonly String DEFAULT_ACTION = "match_questions";

        [Route("api/NLP/QuestionMatch")]
        public IHttpActionResult GetTestQuestion()
        {
            MatchQuestionModelRequest[] actions = new MatchQuestionModelRequest[1];
            MatchQuestionModelRequest nLPAction = new MatchQuestionModelRequest();
            nLPAction.action = DEFAULT_ACTION;
            nLPAction.question_id = -1;
            nLPAction.question = "ROSES ARE RED, VIOLETS ARE BLUE, GANDALF IS A WIZARD, NOW FLY YOU FOOL!";
            nLPAction.compare_questions = new List<NLPQuestionModelInfo>() { new NLPQuestionModelInfo() { question = "TEST QUESTION1" , question_id = -1 } }.ToArray();
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
