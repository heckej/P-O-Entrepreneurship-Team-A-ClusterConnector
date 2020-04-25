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
using ClusterConnector;
using ClusterLogic.Models.ChatbotModels;
using ClusterLogic.ChatbotHandler;

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
            Request.Headers.TryGetValues("Authorization", out IEnumerable<string> res);
            if (!new NLPSecurity().Authenticate(res))
            {
                return new HttpResponseMessage(HttpStatusCode.Forbidden);
            }

            if (true || HttpContext.Current.IsWebSocketRequest)
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
            ProcessNLPResponse.ProcessNLPMatchQuestionsResponse(mqmr);
        }

        public static void Notify()
        {
            if (connections.ContainsKey("NLP") && connections["NLP"] != null && connections["NLP"].State == WebSocketState.Open)
            {
                connections["NLP"].SendAsync(new ArraySegment<byte>(usedEncoding.GetBytes("YO!"), 0, "YO!".Length), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        public static void SendQuestionMatchRequest(MatchQuestionModelRequest mqmr)
        {
            if (connections.ContainsKey("NLP") && connections["NLP"] != null && connections["NLP"].State == WebSocketState.Open && mqmr != null)
            {
                String json = JsonSerializer.Serialize(mqmr);

                connections["NLP"].SendAsync(new ArraySegment<byte>(usedEncoding.GetBytes(json), 0, json.Length), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            else
            {
                taskQueue.Enqueue(new NLPTask<MatchQuestionModelRequest>(mqmr, SendQuestionMatchRequest));
            }
        }

        public static void SendQuestionNonsenseRequest(OffensivenessModelRequest offensivenessModel)
        {
            if (connections.ContainsKey("NLP") && connections["NLP"] != null && connections["NLP"].State == WebSocketState.Open && offensivenessModel!=null)
            {
                String json = JsonSerializer.Serialize(offensivenessModel);

                connections["NLP"].SendAsync(new ArraySegment<byte>(usedEncoding.GetBytes(json), 0, json.Length), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            else
            {
                taskQueue.Enqueue(new NLPTask<OffensivenessModelRequest>(offensivenessModel, SendQuestionNonsenseRequest));
            }
        }


        public static void SendQuestionOffenseRequest(NonsenseLogicResponse offensivenessModel)
        {
            if (connections.ContainsKey("NLP") && connections["NLP"] != null && connections["NLP"].State == WebSocketState.Open && offensivenessModel != null)
            {
                String json = JsonSerializer.Serialize(offensivenessModel);

                connections["NLP"].SendAsync(new ArraySegment<byte>(usedEncoding.GetBytes(json), 0, json.Length), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            else
            {
                taskQueue.Enqueue(new NLPTask<NonsenseLogicResponse>(offensivenessModel, SendQuestionOffenseRequest));
            }
        }

        internal static void SendTestToNLP()
        {
            if (connections.ContainsKey("NLP") && connections["NLP"] != null && connections["NLP"].State == WebSocketState.Open)
            {
                OffensivenessModelRequest omr = new OffensivenessModelRequest();
                omr.msg_id = 65;
                omr.question = "It's fucking snowing!";
                omr.question_id = 25;
                omr.action = "ESTIMATE_OFFENSIVENESS".ToLower();

                String json = JsonSerializer.Serialize(omr);

                connections["NLP"].SendAsync(new ArraySegment<byte>(usedEncoding.GetBytes(json), 0, json.Length), WebSocketMessageType.Text, true, CancellationToken.None);
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


            while (taskQueue.Count > 0)
            {
                taskQueue.Dequeue().DoTask();
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

                if (retVal.Count > 0)
                {
                    String jsonResponse = usedEncoding.GetString(buffer.ToArray(), 0, retVal.Count);

                    HandleResponse(ProcessResponse(jsonResponse));
                }


                if (retVal.CloseStatus != null)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Bye", CancellationToken.None);
                    return;
                }

                //await socket.SendAsync(new ArraySegment<byte>(buffer.Array, 0, retVal.Count), retVal.MessageType, retVal.EndOfMessage, CancellationToken.None);
                // In case an empty line should be sent.
                //await socket.SendAsync(new ArraySegment<byte>(usedEncoding.GetBytes("\r\n"), 0, "\r\n".Length), retVal.MessageType, retVal.EndOfMessage, CancellationToken.None);
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
                    try
                    {
                        {
                            MatchQuestionLogicResponse result = ProcessNLPResponse.ProcessNLPMatchQuestionsResponse(model.Value.Cast<MatchQuestionModelResponse>().ToList().First());
                            
                            if (result != null)
                            {
                                if (ServerUtilities.msgIdToUserID[result.Msg_id] is NewQuestion)
                                {
                                    if (result.Match)
                                    {
                                        ChatbotWebSocketController.SendAnswerToQuestion(new ChatbotNewAnswerModel(result));
                                    }
                                    else
                                    {
                                        var temp = ProcessChatbotLogic.GenerateModelCompareToOpenQuestions((NewQuestion)ServerUtilities.msgIdToUserID[result.Msg_id]);
                                        if (temp != null)
                                        {
                                            NLPWebSocketController.SendQuestionMatchRequest(temp);
                                        }
                                    }
                                }else if (ServerUtilities.msgIdToUserID[result.Msg_id] is NewOpenQuestion)
                                {
                                    if (result.Match)
                                    {
                                        ChatbotWebSocketController.SendAnswerToQuestion(new ChatbotNewAnswerModel(result));
                                    }
                                    else
                                    {
                                        ChatbotWebSocketController.SendAnswerToQuestion(new ServerResponseNoAnswerToQuestion(result, (MatchQuestionModelResponse)model.Value.First(), ProcessChatbotLogic.SaveQuestionToDatabase((NewOpenQuestion)ServerUtilities.msgIdToUserID[result.Msg_id])));
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Trace.Write(e.Message);
                    }
                    break;
                case WEBSOCKET_RESPONSE_TYPE.OFFENSIVENESS:
                    {
                        try
                        {
                            var result = ProcessNLPResponse.ProcessNLPOffensivenessResponse(model.Value.Cast<OffensivenessModelResponse>().ToList().First());
                            if (result is OffensivenessLogicResponse)
                            {
                                ProcessOffenseResult(result);
                            }
                        }
                        catch (Exception e)
                        {
                            System.Diagnostics.Trace.Write(e.Message);
                        }
                    }
                    break;
                case WEBSOCKET_RESPONSE_TYPE.NONSENSE:
                    {
                        try
                        {
                            NonsenseLogicResponse result = ProcessNLPResponse.ProcessNLPNonsenseResponse(model.Value.Cast<NonsenseModelResponse>().ToList().First());
                            if (result is NonsenseLogicResponse)
                            {
                                ProcessNonsenseResult(result);
                                //SendQuestionOffenseRequest((NonsenseLogicResponse)result);
                            }
                        }
                        catch (Exception e)
                        {
                            System.Diagnostics.Trace.Write(e.Message);
                        }
                     }
                    break;
            }
        }

        internal static void CheckIfQuestionIsNonsense(ChatbotNewQuestionModel chatbotNewQuestionModel)
        {
            var temp = new OffensivenessModelRequest(chatbotNewQuestionModel);
            if (connections.ContainsKey("NLP") && connections["NLP"] != null && connections["NLP"].State == WebSocketState.Open && temp != null)
            {
                String json = JsonSerializer.Serialize(temp);

                connections["NLP"].SendAsync(new ArraySegment<byte>(usedEncoding.GetBytes(json), 0, json.Length), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            else
            {
                taskQueue.Enqueue(new NLPTask<ChatbotNewQuestionModel>(chatbotNewQuestionModel, CheckIfQuestionIsNonsense));
            }
        }

        private void ProcessNonsenseResult(NonsenseLogicResponse result)
        {
            if (ServerUtilities.msgIdToUserID[result.Msg_id] is NewAnswerNonsenseCheck)
            {
                if (result.Nonsense)
                {
                    ProcessChatbotLogic.ProcessNonsenseAnswer((NewAnswerNonsenseCheck)ServerUtilities.msgIdToUserID[result.Msg_id]);
                }
                else
                {
                    SendAnswerOffenseRequest((NewAnswerNonsenseCheck)ServerUtilities.msgIdToUserID[result.Msg_id]);
                }
            }
            else if (ServerUtilities.msgIdToUserID[result.Msg_id] is NewQuestionNonsenseCheck)
            {
                if (result.Nonsense)
                {
                    ProcessChatbotLogic.ProcessNonsenseQuestion((NewQuestionNonsenseCheck)ServerUtilities.msgIdToUserID[result.Msg_id]);
                    ChatbotWebSocketController.SendAnswerToQuestion(new ChatbotVariousServerResponses((NewQuestionNonsenseCheck)ServerUtilities.msgIdToUserID[result.Msg_id]));
                }
                else
                {
                    SendQuestionOffenseRequest((NewQuestionNonsenseCheck)ServerUtilities.msgIdToUserID[result.Msg_id]);
                }
            }
        }

        private void ProcessOffenseResult(OffensivenessLogicResponse result)
        {
            if (ServerUtilities.msgIdToUserID[result.Msg_id] is NewAnswerOffenseCheck)
            {
                if (result.Offensive)
                {
                    ProcessChatbotLogic.ProcessOffensiveAnswer((NewAnswerOffenseCheck)ServerUtilities.msgIdToUserID[result.Msg_id]);
                }
                else
                {
                    //ProcessChatbotLogic.SaveQuestionToDatabase((NewQuestionNonsenseCheck)ServerUtilities.msgIdToUserID[result.Msg_id]);
                    ProcessChatbotLogic.SaveAnswerToOpenQuestion((NewAnswerOffenseCheck)ServerUtilities.msgIdToUserID[result.Msg_id]);
                }
            }
            else if (ServerUtilities.msgIdToUserID[result.Msg_id] is NewQuestionNonsenseCheck)
            {
                if (result.Offensive)
                {
                    ProcessChatbotLogic.ProcessOffensiveAnswer((NewQuestionNonsenseCheck)ServerUtilities.msgIdToUserID[result.Msg_id]);
                    ChatbotWebSocketController.SendAnswerToQuestion(new ChatbotVariousServerResponses((NewQuestionNonsenseCheck)ServerUtilities.msgIdToUserID[result.Msg_id], false));
                }
                else
                {
                    ChatbotWebSocketController.ProcessProperQuestion((NewQuestionNonsenseCheck)ServerUtilities.msgIdToUserID[result.Msg_id]);
                }
            }
        }

        private void SendQuestionOffenseRequest(NewQuestionNonsenseCheck newQuestionNonsenseCheck)
        {
            var temp = new OffensivenessModelRequest(newQuestionNonsenseCheck);
            if (connections.ContainsKey("NLP") && connections["NLP"] != null && connections["NLP"].State == WebSocketState.Open && temp != null)
            {
                String json = JsonSerializer.Serialize(temp);

                connections["NLP"].SendAsync(new ArraySegment<byte>(usedEncoding.GetBytes(json), 0, json.Length), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            else
            {
                taskQueue.Enqueue(new NLPTask<NewQuestionNonsenseCheck>(newQuestionNonsenseCheck, SendQuestionOffenseRequest));
            }
        }

        private void SendAnswerOffenseRequest(NewAnswerNonsenseCheck newAnswerNonsenseCheck)
        {
            var temp = new OffensivenessModelRequest(newAnswerNonsenseCheck);
            if (connections.ContainsKey("NLP") && connections["NLP"] != null && connections["NLP"].State == WebSocketState.Open && temp != null)
            {
                String json = JsonSerializer.Serialize(temp);

                connections["NLP"].SendAsync(new ArraySegment<byte>(usedEncoding.GetBytes(json), 0, json.Length), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            else
            {
                taskQueue.Enqueue(new NLPTask<NewAnswerNonsenseCheck>(newAnswerNonsenseCheck,SendAnswerOffenseRequest));
            }
        }

        private void Echo(OffensivenessLogicResponse result)
        {
            if (connections.ContainsKey("NLP") && connections["NLP"] != null && connections["NLP"].State == WebSocketState.Open)
            {
                connections["NLP"].SendAsync(new ArraySegment<byte>(usedEncoding.GetBytes("ECHO ECHO TO NLP"), 0, "ECHO ECHO TO NLP".Length), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        static Queue<ITaskInterface> taskQueue = new Queue<ITaskInterface>();

        public struct NLPTask<T> : ITaskInterface
        {
            T model;
            public delegate void TaskMethod(T t);
            TaskMethod taskMethod;

            public NLPTask(T model, TaskMethod tm)
            {
                taskMethod = tm;
                this.model = model;
            }

            public void DoTask()
            {
                taskMethod.Invoke(model);
            }
        }
    }
}
