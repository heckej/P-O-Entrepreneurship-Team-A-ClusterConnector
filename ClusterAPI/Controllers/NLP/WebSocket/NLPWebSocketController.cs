using System.Text.Json;
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
using ClusterLogic.Models;
using ClusterLogic.NLPHandler;
using ClusterAPI.Controllers.Security;

namespace ClusterAPI.Controllers.NLP
{
    public class NLPWebSocketController : ApiController
    {
        private static readonly String DEFAULT_ACTION = "match_questions";
        private enum WEBSOCKET_RESPONSE_TYPE { OFFENSIVENESS, NONSENSE, MATCH_QUESTION, NONE }
        private static readonly Encoding usedEncoding = Encoding.UTF8;
        private static readonly Dictionary<String, WebSocket> connections = new Dictionary<string, WebSocket>();

        [HttpGet]
        [Route("api/NLP/WS")]
        public HttpResponseMessage GetMessage()

        {
            if (!new NLPSecurity().Authenticate(Request.Headers.GetValues("Authorization")))
            {
                return new HttpResponseMessage(HttpStatusCode.Forbidden);
            }

            if (HttpContext.Current.IsWebSocketRequest)
            {

                HttpContext.Current.AcceptWebSocketRequest(ProcessRequestInternal);

            }

            return new HttpResponseMessage(HttpStatusCode.SwitchingProtocols);
        }

        public async static void TestMatchQuestion()
        {
            MatchQuestionModelResponse mqmr = new MatchQuestionModelResponse();
            mqmr.msg_id = 67;
            mqmr.possible_matches = new MatchQuestionModelInfo[] {
                new MatchQuestionModelInfo() { prob = .85f, question_id = 5},
                new MatchQuestionModelInfo() { prob = .6f, question_id = 7},
                new MatchQuestionModelInfo() { prob = .15f, question_id = 1}
            };
            mqmr.question_id = 23;
            ProcessNLPResponse.ProcessNLPMatchQuestionsResponse(new List<MatchQuestionModelResponse>() { mqmr });
        }

        public async static void Notify()
        {
            if (connections.ContainsKey("NLP") && connections["NLP"] != null && connections["NLP"].State == WebSocketState.Open)
            {
                await connections["NLP"].SendAsync(new ArraySegment<byte>(usedEncoding.GetBytes("YO!"), 0, "YO!".Length), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        public async static void SendQuestionMatchRequest(params Object[] args)
        {
            if (connections.ContainsKey("NLP") && connections["NLP"] != null && connections["NLP"].State == WebSocketState.Open)
            {
                String json = new QuestionMatchRequestGen().GenerateRequest(args);

                await connections["NLP"].SendAsync(new ArraySegment<byte>(usedEncoding.GetBytes(json), 0, json.Length), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        public async static void SendQuestionNonsenseRequest(OffensivenessModelRequest offensivenessModel)
        {
            if (connections.ContainsKey("NLP") && connections["NLP"] != null && connections["NLP"].State == WebSocketState.Open)
            {
                String json = JsonSerializer.Serialize(offensivenessModel);

                await connections["NLP"].SendAsync(new ArraySegment<byte>(usedEncoding.GetBytes(json), 0, json.Length), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }


        public async static void SendQuestionOffenseRequest(OffensivenessModelRequest offensivenessModel)
        {
            if (connections.ContainsKey("NLP") && connections["NLP"] != null && connections["NLP"].State == WebSocketState.Open)
            {
                String json = JsonSerializer.Serialize(offensivenessModel);

                await connections["NLP"].SendAsync(new ArraySegment<byte>(usedEncoding.GetBytes(json), 0, json.Length), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        private async Task ProcessRequestInternal(AspNetWebSocketContext ctx)
        {
            System.Diagnostics.Debug.WriteLine("In socket");

            WebSocket socket = ctx.WebSocket;

            if (connections.Count == 0)
            {
                connections.Add("NLP",socket);
            }
            else
            {
                try
                {
                    if (connections.ContainsKey("NLP") && connections["NLP"] != null && connections["NLP"].State == WebSocketState.Open)
                    {
                        await connections["NLP"].CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Bye", CancellationToken.None);
                        await connections["NLP"].CloseAsync(WebSocketCloseStatus.NormalClosure, "Bye", CancellationToken.None);
                        connections.Remove("NLP");
                        connections.Add("NLP", socket);
                    }
                }
                catch(ObjectDisposedException e)
                {
                    connections.Remove("NLP");
                    connections.Add("NLP", socket);
                }
            }

            await socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("Hello NLP")), WebSocketMessageType.Text, true, CancellationToken.None);

            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[2048]);
            while (true)
            {
                WebSocketReceiveResult retVal = null;
                try
                {
                    retVal = await socket.ReceiveAsync(buffer, CancellationToken.None);
                }
                catch (OperationCanceledException e)
                {
                    continue;
                }

                if (retVal == null || retVal.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Bye", CancellationToken.None);
                    break;
                }

                String jsonResponse = usedEncoding.GetString(buffer.ToArray(),0,retVal.Count);

                HandleResponse(ProcessResponse(jsonResponse));

                if (retVal.CloseStatus != null)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Bye", CancellationToken.None);
                    return;
                }

                //await socket.SendAsync(new ArraySegment<byte>(buffer.Array, 0, retVal.Count), retVal.MessageType, retVal.EndOfMessage, CancellationToken.None);
                await socket.SendAsync(new ArraySegment<byte>(usedEncoding.GetBytes("\r\n"), 0, "\r\n".Length), retVal.MessageType, retVal.EndOfMessage, CancellationToken.None);
            }
        }

        private KeyValuePair<WEBSOCKET_RESPONSE_TYPE, List<BaseModel>> ProcessResponse(String jsonResponse)
        {
            if (jsonResponse == null || jsonResponse.Equals(""))
            {
                return new KeyValuePair<WEBSOCKET_RESPONSE_TYPE, List<BaseModel>>(WEBSOCKET_RESPONSE_TYPE.NONE,null);
            }

            try
            {
                var result = JsonSerializer.Deserialize<MatchQuestionModelResponse>(jsonResponse);
                if (!result.IsComplete())
                {
                    throw new Exception();
                }
                return new KeyValuePair<WEBSOCKET_RESPONSE_TYPE, List<BaseModel>>(WEBSOCKET_RESPONSE_TYPE.MATCH_QUESTION, new List<BaseModel>() { result });
            }
            catch {

            }



            try
            {
                var result = JsonSerializer.Deserialize<OffensivenessModelResponse>(jsonResponse);
                if (!result.IsComplete())
                {
                    throw new Exception();
                }
                return new KeyValuePair<WEBSOCKET_RESPONSE_TYPE, List<BaseModel>>(WEBSOCKET_RESPONSE_TYPE.OFFENSIVENESS, new List<BaseModel>() { result });
            }
            catch
            {

            }

            try
            {
                var result = JsonSerializer.Deserialize<NonsenseModelResponse>(jsonResponse);
                if (!result.IsComplete())
                {
                    throw new Exception();
                }
                return new KeyValuePair<WEBSOCKET_RESPONSE_TYPE, List<BaseModel>>(WEBSOCKET_RESPONSE_TYPE.NONSENSE, new List<BaseModel>() { result });
            }
            catch
            {

            }

            return new KeyValuePair<WEBSOCKET_RESPONSE_TYPE, List<BaseModel>>(WEBSOCKET_RESPONSE_TYPE.NONE, null);
        }

        private void HandleResponse(KeyValuePair<WEBSOCKET_RESPONSE_TYPE, List<BaseModel>> model)
        {
            //TODO: what should the websocket do with bad responses?
            if (model.Key == WEBSOCKET_RESPONSE_TYPE.NONE)
            {
                return;
            }

            switch (model.Key)
            {
                case WEBSOCKET_RESPONSE_TYPE.MATCH_QUESTION:
                    ProcessNLPResponse.ProcessNLPMatchQuestionsResponse(model.Value.Cast<MatchQuestionModelResponse>().ToList());
                    break;
                case WEBSOCKET_RESPONSE_TYPE.OFFENSIVENESS:
                    ProcessNLPResponse.ProcessNLPOffensivenessResponse(model.Value.Cast<OffensivenessModelResponse>().ToList());
                    break;
                case WEBSOCKET_RESPONSE_TYPE.NONSENSE:
                    var result = ProcessNLPResponse.ProcessNLPNonsenseResponse(model.Value.Cast<NonsenseModelResponse>().ToList());
                    if (result is OffensivenessModelRequest)
                    {
                        SendQuestionOffenseRequest((OffensivenessModelRequest)result);
                    }
                    break;
            }
        }
    }
}
