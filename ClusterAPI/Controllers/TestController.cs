using ClusterConnector.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace ClusterAPI.Controllers
{
    public class TestController : ApiController
    {

        public IHttpActionResult GetTestQuestion(int id)
        {
            var result = TestQuestion.TestConnection(id);
            if (result == null || result.Count == 0)
            {
                return NotFound();
            }
            return Ok(TestQuestion.TestConnection(id));
        }
    }
}
