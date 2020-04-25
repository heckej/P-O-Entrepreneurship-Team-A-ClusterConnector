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
using ClusterLogic.Models.ChatbotModels;
using ClusterLogic.ChatbotHandler;
using ClusterConnector;
using ClusterConnector.Models.Database;
using ClusterConnector.Processors;

namespace ClusterAPI.Controllers.NLP
{
    public class ChatbotWebSocketController : ApiController
    {
        private static readonly String DEFAULT_ACTION = "match_questions";
        private enum WEBSOCKET_RESPONSE_TYPE { NEW_QUESTION ,REQUEST_ANSWER_TO_QUESTION, RECEIVE_ANSWER, REQUEST_UNANSWERED_QUESTIONS , NONE }
        private static readonly Encoding usedEncoding = Encoding.UTF8;
        private static readonly Dictionary<String, WebSocket> connections = new Dictionary<string, WebSocket>();

        [HttpGet]
        [Route("api/Chatbot/WS")]
        public HttpResponseMessage GetMessage()
        {
            Request.Headers.TryGetValues("Authorization", out IEnumerable<string> res);
            if (!new ChatbotSecurity().Authenticate(Request.Headers.GetValues("Authorization")))
            {
                return new HttpResponseMessage(HttpStatusCode.Forbidden);
            }

            if (HttpContext.Current.IsWebSocketRequest)
            {
                HttpContext.Current.AcceptWebSocketRequest(ProcessRequestInternal);
            }

            return new HttpResponseMessage(HttpStatusCode.SwitchingProtocols);
        }

        public async static void Notify()
        {
            if (connections.ContainsKey("Chatbot") && connections["Chatbot"] != null && connections["Chatbot"].State == WebSocketState.Open)
            {
                await connections["Chatbot"].SendAsync(new ArraySegment<byte>(usedEncoding.GetBytes("YO!"), 0, "YO!".Length), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        /// <summary>
        /// Send a message to the NLP Controller to process the given answer and check if the contents are nonsone or not.
        /// 
        /// </summary>
        /// <param name="result"></param>
        private void SendAnswerToNLPForNonsense(OffensivenessModelRequest result)
        {
            NLPWebSocketController.SendQuestionNonsenseRequest(result);
        }

        public static void SendAnswerToQuestion(List<ChatbotAnswerRequestResponseModel> result)
        {
            if (connections.ContainsKey("Chatbot") && connections["Chatbot"] != null && connections["Chatbot"].State == WebSocketState.Open)
            {
                String json = JsonSerializer.Serialize<ChatbotAnswerRequestResponseModel[]>(result.ToArray());
                connections["Chatbot"].SendAsync(new ArraySegment<byte>(usedEncoding.GetBytes(json), 0, json.Length), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        internal static void SendAnswerToQuestion(ServerResponseNoAnswerToQuestion serverResponseNoAnswerToQuestion)
        {
            if (connections.ContainsKey("Chatbot") && connections["Chatbot"] != null && connections["Chatbot"].State == WebSocketState.Open)
            {
                String json = JsonSerializer.Serialize<ServerResponseNoAnswerToQuestion>(serverResponseNoAnswerToQuestion);
                connections["Chatbot"].SendAsync(new ArraySegment<byte>(usedEncoding.GetBytes(json), 0, json.Length), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        internal static void SendAnswerToQuestion(ChatbotVariousServerResponses chatbotVariousServerResponses)
        {
            if (connections.ContainsKey("Chatbot") && connections["Chatbot"] != null && connections["Chatbot"].State == WebSocketState.Open)
            {
                String json = JsonSerializer.Serialize<ChatbotVariousServerResponses>(chatbotVariousServerResponses);
                connections["Chatbot"].SendAsync(new ArraySegment<byte>(usedEncoding.GetBytes(json), 0, json.Length), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        public static void SendUnansweredQuestions(List<ChatbotRequestUnansweredQuestionsResponseModel> result)
        {
            if (connections.ContainsKey("Chatbot") && connections["Chatbot"] != null && connections["Chatbot"].State == WebSocketState.Open)
            {
                String json = JsonSerializer.Serialize<ChatbotRequestUnansweredQuestionsResponseModel[]>(result.ToArray());
                connections["Chatbot"].SendAsync(new ArraySegment<byte>(usedEncoding.GetBytes(json), 0, json.Length), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        private void SendUnansweredQuestions(ChatbotResponseUnansweredQuestionsModelToChatbot result)
        {
            if (connections.ContainsKey("Chatbot") && connections["Chatbot"] != null && connections["Chatbot"].State == WebSocketState.Open)
            {
                String json = JsonSerializer.Serialize<ChatbotResponseUnansweredQuestionsModelToChatbot>(result);
                connections["Chatbot"].SendAsync(new ArraySegment<byte>(usedEncoding.GetBytes(json), 0, json.Length), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        internal static void SendAnswerToQuestion(ChatbotNewAnswerModel chatbotNewAnswerModel)
        {
            if (connections.ContainsKey("Chatbot") && connections["Chatbot"] != null && connections["Chatbot"].State == WebSocketState.Open)
            {
                String json = JsonSerializer.Serialize<ChatbotNewAnswerModel>(chatbotNewAnswerModel);
                connections["Chatbot"].SendAsync(new ArraySegment<byte>(usedEncoding.GetBytes(json), 0, json.Length), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        private async Task ProcessRequestInternal(AspNetWebSocketContext ctx)
        {
            System.Diagnostics.Debug.WriteLine("In chatbot socket");

            WebSocket socket = ctx.WebSocket;

            if (connections.Count == 0)
            {
                connections.Add("Chatbot",socket);
            }
            else
            {
                try
                {
                    if (connections.ContainsKey("Chatbot") && connections["Chatbot"] != null && connections["Chatbot"].State == WebSocketState.Open)
                    {
                        await connections["Chatbot"].CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Bye", CancellationToken.None);
                        await connections["Chatbot"].CloseAsync(WebSocketCloseStatus.NormalClosure, "Bye", CancellationToken.None);
                        connections.Remove("Chatbot");
                        connections.Add("Chatbot", socket);
                    }
                }
                catch(ObjectDisposedException e)
                {
                    connections.Remove("Chatbot");
                    connections.Add("Chatbot", socket);
                }
            }

            await socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("Hello Chatbot")), WebSocketMessageType.Text, true, CancellationToken.None);

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
                var dict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonResponse);

                if (dict.ContainsKey("user_id"))
                    // if user not in database, add user
                    try
                    {
                        AddUserToDatabaseIfNonExisting(dict["user_id"]);
                    }
                    catch(Exception e)
                    {
                        Console.Error.WriteLine(e.Message);
                    }

                if (dict.Keys.Contains<String>("user_id") &&
                    dict.Keys.Contains<String>("question") &&
                    dict.Keys.Contains<String>("chatbot_temp_id") && dict.Count == 3)
                {
                    return new KeyValuePair<WEBSOCKET_RESPONSE_TYPE, List<BaseModel>>(WEBSOCKET_RESPONSE_TYPE.NEW_QUESTION, new List<BaseModel>() { new ChatbotNewQuestionModel(dict) });
                }
            }
            catch (Exception)
            {

            }

            try
            {
                var result = JsonSerializer.Deserialize<ChatbotNewQuestionModel>(jsonResponse);
                if (!result.IsComplete())
                {
                    throw new Exception();
                }
                return new KeyValuePair<WEBSOCKET_RESPONSE_TYPE, List<BaseModel>>(WEBSOCKET_RESPONSE_TYPE.REQUEST_ANSWER_TO_QUESTION, new List<BaseModel>() { result });
            }
            catch
            {

            }

            try
            {
                var result = JsonSerializer.Deserialize<ChatbotAnswerRequestModel>(jsonResponse);
                if (!result.IsComplete())
                {
                    throw new Exception();
                }
                return new KeyValuePair<WEBSOCKET_RESPONSE_TYPE, List<BaseModel>>(WEBSOCKET_RESPONSE_TYPE.REQUEST_ANSWER_TO_QUESTION, new List<BaseModel>() { result });
            }
            catch {

            }

            try
            {
                var result = JsonSerializer.Deserialize<ChatbotGivesAnswersToQuestionsToServer>(jsonResponse);
                if (!result.IsComplete())
                {
                    throw new Exception();
                }
                return new KeyValuePair<WEBSOCKET_RESPONSE_TYPE, List<BaseModel>>(WEBSOCKET_RESPONSE_TYPE.RECEIVE_ANSWER, new List<BaseModel>() { result });
            }
            catch
            {

            }

            try
            {
                var result = JsonSerializer.Deserialize<ChatbotRequestUnansweredQuestionsModel>(jsonResponse);
                if (!result.IsComplete())
                {
                    throw new Exception();
                }
                return new KeyValuePair<WEBSOCKET_RESPONSE_TYPE, List<BaseModel>>(WEBSOCKET_RESPONSE_TYPE.REQUEST_UNANSWERED_QUESTIONS, new List<BaseModel>() { result });
            }
            catch
            {

            }


            return new KeyValuePair<WEBSOCKET_RESPONSE_TYPE, List<BaseModel>>(WEBSOCKET_RESPONSE_TYPE.NONE, null);
        }

        private void AddUserToDatabaseIfNonExisting(string userID)
        {
            DBUserProcessor userProcessor = new DBUserProcessor();
            DBUser userData = userProcessor.getByKey(userID);
            // if userData null, add user
            if(userData == null)
                userProcessor.AddUser(userID);
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
                case WEBSOCKET_RESPONSE_TYPE.NEW_QUESTION:
                    {
                        try
                        {
                            NLPWebSocketController.CheckIfQuestionIsNonsense(model.Value.Cast<ChatbotNewQuestionModel>().First());
                        }
                        catch (Exception e)
                        {
                            System.Diagnostics.Trace.Write(e.Message);
                        }
                    }
                    break;
                case WEBSOCKET_RESPONSE_TYPE.RECEIVE_ANSWER:
                    {
                        try
                        {
                            foreach (ChatbotGivesAnswerModelToServer item in model.Value.Cast<ChatbotGivesAnswersToQuestionsToServer>().First().answer_questions)
                            {
                                var result = ProcessChatbotLogic.ProcessChatbotReceiveAnswer(item, model.Value.Cast<ChatbotGivesAnswersToQuestionsToServer>().First());

                                if (result != null)
                                {
                                    SendAnswerToNLPForNonsense(result);
                                }
                            }
                            
                        }
                        catch (Exception e)
                        {
                            System.Diagnostics.Trace.Write(e.Message);
                        }
                    }
                    break;
                case WEBSOCKET_RESPONSE_TYPE.REQUEST_ANSWER_TO_QUESTION:
                    {
                        try
                        {
                            var result = ProcessChatbotLogic.ProcessChatbotRequestAnswerToQuestion(model.Value.Cast<ChatbotAnswerRequestModel>().ToList());
                            if (result != null)
                            {
                                SendAnswerToQuestion(result);
                            }
                        }
                        catch (Exception e)
                        {
                            System.Diagnostics.Trace.Write(e.Message);
                        }
                    }
                    break;
                case WEBSOCKET_RESPONSE_TYPE.REQUEST_UNANSWERED_QUESTIONS:
                    {
                        try
                        {
                            var result = ProcessChatbotLogic.RetrieveOpenQuestions(model.Value.Cast<ChatbotRequestUnansweredQuestionsModel>().First().user_id);
                            if (result != null)
                            {
                                SendUnansweredQuestions(new ChatbotResponseUnansweredQuestionsModelToChatbot(result));
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

        internal static void ProcessProperQuestion(NewQuestionNonsenseCheck newQuestionNonsenseCheck)
        {
            try
            {
                var result = ProcessChatbotLogic.ProcessChatbotReceiveQuestion(new List<ChatbotNewQuestionModel>() { new ChatbotNewQuestionModel(newQuestionNonsenseCheck)});
                if (result != null)
                {
                    NLPWebSocketController.SendQuestionMatchRequest(result);
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.Write(e.Message);
            }
        }
    }
}
