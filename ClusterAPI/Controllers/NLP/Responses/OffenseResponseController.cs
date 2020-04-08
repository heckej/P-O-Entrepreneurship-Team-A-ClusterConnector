using ClusterLogic.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace ClusterAPI.Controllers.NLP
{
    public class OffenseResponseController : ApiController
    {

        [Route("api/NLP/QuestionOffensivesness")]
        public IHttpActionResult PostResult(OffensivenessModelResponse offensiveModel)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid data.");

            //TODO: process this

            return Ok();
        }
    }
}
