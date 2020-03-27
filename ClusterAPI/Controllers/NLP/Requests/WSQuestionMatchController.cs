using ClusterConnector.Models.NLP;
using System.Text.Json;
using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.WebSockets;

namespace ClusterAPI.Controllers.NLP
{
    public class WSQuestionMatchController : ApiController
    {
        private static readonly String DEFAULT_ACTION = "Answers.MATCH_QUESTIONS";

        [HttpGet]
        [Route("api/NLP/WS/QuestionMatch")]
        public HttpResponseMessage GetMessage()

        {

            if (HttpContext.Current.IsWebSocketRequest)

            {

                HttpContext.Current.AcceptWebSocketRequest(ProcessRequestInternal);

            }

            return new HttpResponseMessage(HttpStatusCode.SwitchingProtocols);

        }

        //[Route("api/NLP/QuestionMatch")]
        //public IHttpActionResult GetTestQuestion()
        //{
        //    NLPActionQuestionMatch nLPAction = new NLPActionQuestionMatch();
        //    nLPAction.Action = DEFAULT_ACTION;
        //    nLPAction.Question_id = -1;
        //    nLPAction.Question = "ROSES ARE RED, VIOLETS ARE BLUE, GANDALF IS A WIZARD, NOW FLY YOU FOOL!";
        //    nLPAction.Compare_questions = new List<ClusterConnector.Models.NLP.NLPQuestion>() { new NLPQuestion() { Question = "TEST QUESTION1" , Question_id = -1 } };
        //    nLPAction.Msg_id = -1;

        //    if (nLPAction == null)
        //    {
        //        return NotFound();
        //    }
        //    var temp = Ok(nLPAction);
        //    return Ok(nLPAction);
        //}

        private async Task ProcessRequestInternal(AspNetWebSocketContext ctx)

        {
            System.Diagnostics.Debug.WriteLine("In socket");

            WebSocket socket = ctx.WebSocket;

            await socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("Hello")), WebSocketMessageType.Text, true, CancellationToken.None);

            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[2048]);
            while (true)
            {
                var retVal = await socket.ReceiveAsync(buffer, CancellationToken.None);

                NLPActionQuestionMatch nLPAction = new NLPActionQuestionMatch();
                nLPAction.Action = DEFAULT_ACTION;
                nLPAction.Question_id = -1;
                nLPAction.Question = "ROSES ARE RED, VIOLETS ARE BLUE, GANDALF IS A WIZARD, NOW FLY YOU FOOL!";
                nLPAction.Compare_questions = new List<ClusterConnector.Models.NLP.NLPQuestion>() { new NLPQuestion() { Question = "TEST QUESTION1", Question_id = -1 } };
                nLPAction.Msg_id = -1;

                string jsonString = JsonSerializer.Serialize(nLPAction);


                if (retVal.CloseStatus != null)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Bye", CancellationToken.None);
                    return;
                }

                //await socket.SendAsync(new ArraySegment<byte>(buffer.Array, 0, retVal.Count), retVal.MessageType, retVal.EndOfMessage, CancellationToken.None);
                await socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(jsonString), 0, jsonString.Length), retVal.MessageType, retVal.EndOfMessage, CancellationToken.None);
            }

        }
    }
}
