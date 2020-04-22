using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace ClusterAPI.Controllers.NLP.Requests
{
    public class TestController : ApiController
    {
        [Route("api/NLP/Test")]
        public IHttpActionResult GetTest()
        {
            NLPWebSocketController.TestMatchQuestion();
            return Ok();
        }
    }
}
