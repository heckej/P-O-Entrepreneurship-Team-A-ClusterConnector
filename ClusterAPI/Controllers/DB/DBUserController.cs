using ClusterAPI.Utilities;
using ClusterConnector.Processors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.ModelBinding;

namespace ClusterAPI.Controllers.DB
{
    public class DBUserController : ApiController
    {
        public IHttpActionResult GetDBUser(int user_id)
        {
            DBUserProcessor dBUserProcessor = new DBUserProcessor();
            var result = dBUserProcessor.getByKey(user_id);

            if (result == null)
            {
                return NotFound();
            }
            return Ok(result);
        }

        //Example on how to handle an array request
        //With curl:
        //curl http://localhost:39160/api/DBAnswer?answer_ids=999,998
        public IHttpActionResult GetDBAnswer([ModelBinder(typeof(CommaDelimitedArrayModelBinder))] int[] user_ids)
        {
            DBUserProcessor dBUserProcessor = new DBUserProcessor();
            var result = dBUserProcessor.getByKeys(user_ids.ToList());

            if (result == null)
            {
                return NotFound();
            }
            return Ok(result);
        }
    }
}
