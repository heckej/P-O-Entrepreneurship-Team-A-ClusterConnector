using ClusterAPI.Utilities;
using ClusterConnector.Processors;
using System.Linq;
using System.Web.Http;
using System.Web.Http.ModelBinding;

namespace ClusterAPI.Controllers.DB
{
    public class DBAnswerController : ApiController
    {
        public IHttpActionResult GetDBAnswer(int answer_id)
        {
            DBAnswerProcessor dBAnswerProcessor = new DBAnswerProcessor();
            var result = dBAnswerProcessor.getByKey(answer_id);

            if (result == null)
            {
                return NotFound();
            }
            return Ok(result);
        }

        //Example on how to handle an array request
        //With curl:
        //curl http://localhost:39160/api/DBAnswer?answer_ids=999,998
        public IHttpActionResult GetDBAnswer([ModelBinder(typeof(CommaDelimitedArrayModelBinder))] int[] answer_ids)
        {
            DBAnswerProcessor dBAnswerProcessor = new DBAnswerProcessor();
            var result = dBAnswerProcessor.getByKeys(answer_ids.ToList());

            if (result == null)
            {
                return NotFound();
            }
            return Ok(result);
        }
    }
}
