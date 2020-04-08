using ClusterLogic.Models;
using System.Web.Http;

namespace ClusterAPI.Controllers.NLP
{
    public class QuestionMatchResponseController : ApiController
    {
        
        [Route("api/NLP/QuestionsMatch")]
        public IHttpActionResult PostResult(MatchQuestionModelResponse model)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid data.");

            //TODO: process this

            return Ok();
        }
    }
}
