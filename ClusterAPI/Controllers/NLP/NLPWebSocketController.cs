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
using ClusterLogic.Models;
using ClusterLogic.NLPHandler;

namespace ClusterAPI.Controllers.NLP
{
    public class NLPWebSocketController : ApiController
    {
        private static readonly String DEFAULT_ACTION = "match_questions";
        private enum WEBSOCKET_RESPONSE_TYPE { OFFENSIVENESS, MATCH_QUESTION, NONE }
        private static readonly Encoding usedEncoding = Encoding.UTF8;
        private static readonly Dictionary<String, WebSocket> connections = new Dictionary<string, WebSocket>();

        [HttpGet]
        [Route("api/NLP/WS")]
        public HttpResponseMessage GetMessage()

        {

            if (HttpContext.Current.IsWebSocketRequest)
            {

                HttpContext.Current.AcceptWebSocketRequest(ProcessRequestInternal);

            }

            return new HttpResponseMessage(HttpStatusCode.SwitchingProtocols);
        }

        public async static void Notify()
        {
            if (connections.ContainsKey("NLP") && connections["NLP"] != null && connections["NLP"].State == WebSocketState.Open)
            {
                await connections["NLP"].SendAsync(new ArraySegment<byte>(usedEncoding.GetBytes("YO!"), 0, "YO!".Length), WebSocketMessageType.Text, true, CancellationToken.None);
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

            await socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("Hello")), WebSocketMessageType.Text, true, CancellationToken.None);

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

                NLPActionQuestionMatch[] actions = new NLPActionQuestionMatch[1];
                NLPActionQuestionMatch nLPAction = new NLPActionQuestionMatch();
                nLPAction.Action = DEFAULT_ACTION;
                nLPAction.Question_id = -1;
                nLPAction.Question = "ROSES ARE RED, VIOLETS ARE BLUE, GANDALF IS A WIZARD, NOW FLY YOU FOOL!";
                nLPAction.Compare_questions = new List<ClusterConnector.Models.NLP.NLPQuestion>() { new NLPQuestion() { Question = "TEST QUESTION1", Question_id = -1 } };
                nLPAction.Msg_id = -1;

                actions[0] = nLPAction;

                string jsonString = JsonSerializer.Serialize(actions);


                if (retVal.CloseStatus != null)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Bye", CancellationToken.None);
                    return;
                }

                //await socket.SendAsync(new ArraySegment<byte>(buffer.Array, 0, retVal.Count), retVal.MessageType, retVal.EndOfMessage, CancellationToken.None);
                await socket.SendAsync(new ArraySegment<byte>(usedEncoding.GetBytes(jsonString), 0, jsonString.Length), retVal.MessageType, retVal.EndOfMessage, CancellationToken.None);
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
                var result = JsonSerializer.Deserialize<MatchQuestionModel>(jsonResponse);
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
                var result = JsonSerializer.Deserialize<MatchQuestionModel[]>(jsonResponse);
                foreach (var item in result)
                {
                    if (!item.IsComplete())
                    {
                        throw new Exception();
                    }
                }
                return new KeyValuePair<WEBSOCKET_RESPONSE_TYPE, List<BaseModel>>(WEBSOCKET_RESPONSE_TYPE.MATCH_QUESTION, result.ToList<BaseModel>());
            }
            catch
            {

            }

            try
            {
                var result = JsonSerializer.Deserialize<OffensivenessModel>(jsonResponse);
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
                var result = JsonSerializer.Deserialize<OffensivenessModel[]>(jsonResponse);
                foreach (var item in result)
                {
                    if (!item.IsComplete())
                    {
                        throw new Exception();
                    }
                }
                return new KeyValuePair<WEBSOCKET_RESPONSE_TYPE, List<BaseModel>>(WEBSOCKET_RESPONSE_TYPE.OFFENSIVENESS, result.ToList<BaseModel>());
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
                    ProcessNLPResponse.ProcessNLPMatchQuestionsResponse(model.Value.Cast<MatchQuestionModel>().ToList());
                    break;
                case WEBSOCKET_RESPONSE_TYPE.OFFENSIVENESS:
                    ProcessNLPResponse.ProcessNLPOffensivenessResponse(model.Value.Cast<OffensivenessModel>().ToList());
                    break;
            }
        }
    }
}
