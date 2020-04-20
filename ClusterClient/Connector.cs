using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClusterClient.Models;

namespace ClusterClient
{
    /// <summary>
    /// The class <c>Connector</c> allows communication with the Cluster API server by retrieving and sending questions and answers.
    /// </summary>
    public class Connector
    {
        /*********************************************
         * Constructors
         ********************************************/

        /// <summary>
        /// Initializes a new connector instance with the given websocket host URI used for its websocket connection and the given timeout set as the timeout
        /// before giving up on trying to connect to the server.
        /// </summary>
        /// <param name="webSocketHostURI">The URI referencing the server address to which a websocket connection should be made.</param>
        /// <param name="webSocketConnectionTimeout">The timeout to be set in seconds for the websocket connection before giving up. By default set to 10 seconds.</param>
        /// <param name="authorization">The value to be set for the Authorization header in the initial web socket connection request.</param>
        public Connector(string authorization, string webSocketHostURI = "wss://clusterapi20200320113808.azurewebsites.net/api/Chatbot/WS", int webSocketConnectionTimeout = 10)
        {
            this.authorization = authorization;
            this.webSocketHostURI = new Uri(webSocketHostURI);
            this.webSocketConnectionTimeout = webSocketConnectionTimeout;
            Console.WriteLine("Initialize websocket.");
            this.InitializeWebSocketThread();
        }


        /*********************************************
         * WebSocket
         ********************************************/

        /// <summary>
        /// The URI to be used to make a websocket connection to the server
        /// </summary>
        private Uri webSocketHostURI;

        /// <summary>
        /// The timeout to be set in seconds for the websocket connection before giving up on trying to connect to the server.
        /// </summary>
        private int webSocketConnectionTimeout = 10;

        /// <summary>
        /// The value to be set for the Authorization header in the initial web socket connection request.
        /// </summary>
        private readonly string authorization;

        /// <summary>
        /// Variable referencing the websocket communicator instance in which the websocket connection runs.
        /// </summary>
        private WebSocketCommunicator webSocketCommunicator;

        /// <summary>
        /// Variable referencing the thread in which the websocket communicator runs.
        /// </summary>
        private Thread webSocketConnectionThread;

        /// <summary>
        /// Variable referencing the queue in which messages that are waiting to be sent are stored
        /// </summary>
        private readonly Queue<string> messagesToBeSent = new Queue<string>();

        /// <summary>
        /// Variable referencing the dictionary in which messages from the server are stored by message action until they are read.
        /// </summary>
        /// <list type="table">
        ///     <item>
        ///         <term>Invar</term>
        ///         <description>The set belonging to a certain action can only contain instances of the ServerMessage subclass related to that
        ///         action. E.g. Questions can contain instances of ServerQuestionsMessage, answer of ServerAnswer.</description>
        ///     </item>
        /// </list>
        private readonly IDictionary<string, Dictionary<string, ISet<ServerMessage>>> receivedMessages = new Dictionary<string, Dictionary<string, ISet<ServerMessage>>>()
            {
                { Actions.Default, new Dictionary<string, ISet<ServerMessage>>() },
                { Actions.Questions, new Dictionary<string, ISet<ServerMessage>>() },
                { Actions.Answer, new Dictionary<string, ISet<ServerMessage>>() }
            };

        /// <summary>
        /// Variable referencing a queue in which exceptions thrown by the websocket thread are passed to this <c>Connector</c> instance.
        /// </summary>
        private readonly Queue<Exception> exceptionsFromWebSocketCommunicator = new Queue<Exception>();

        /// <summary>
        /// Variable referencing a cancelation token source used to control tasks.
        /// </summary>
        private CancellationTokenSource cancellationTokenSource;

        /// <summary>
        /// Resets the websocket thread by stopping the current thread and starting a new one.
        /// <list type="table">
        ///     <item>
        ///         <term>Post</term>
        ///         <description>If a websocket thread was running, it is stopped and replaced by a new websocket thread running a new connection.
        ///         If no websocket thread was running, a new one is initialized running a connection.</description>
        ///     </item>
        /// </list>
        /// </summary>
        public void ResetConnection()
        {
            this.InitializeWebSocketThread();
        }

        /// <summary>
        /// Initializes a new websocket thread.
        /// </summary>
        /// <list type="table">
        ///     <item>
        ///         <term>Post</term>
        ///         <description>If a websocket thread was running, it is stopped and replaced by a new websocket thread running a new connection.
        ///         If no websocket thread was running, a new one is initialized running a connection.</description>
        ///     </item>
        /// </list>
        /// </summary>
        private void InitializeWebSocketThread()
        {
            if (this.webSocketCommunicator != null)
            {
                Console.WriteLine("Thread state at initialize thread in null check: " + this.webSocketConnectionThread.ThreadState);
                Console.WriteLine("Stop websocket.");
                this.cancellationTokenSource.Cancel();
            }
            // create new cancellation token source
            this.cancellationTokenSource = new CancellationTokenSource();
            Console.WriteLine("Exception queue length: " + this.exceptionsFromWebSocketCommunicator.Count);
            Console.WriteLine("Clearing exception queue.");
            Debug.WriteLine("Clearing exception queue.");
            this.exceptionsFromWebSocketCommunicator.Clear();
            Console.WriteLine("Starting new thread.");
            Debug.WriteLine("Starting new thread.");
            this.webSocketCommunicator = new WebSocketCommunicator(this.webSocketHostURI, this.exceptionsFromWebSocketCommunicator, 
                                                        this.StoreMessageFromServer, this.messagesToBeSent, this.webSocketConnectionTimeout, this.cancellationTokenSource.Token, this.authorization);
            
            this.webSocketConnectionThread = new Thread(new ThreadStart(this.webSocketCommunicator.Run));
            this.webSocketConnectionThread.IsBackground = true;
            this.webSocketConnectionThread.Start();

            Console.WriteLine("Thread " + this.webSocketConnectionThread.Name + " started.");
            Debug.WriteLine("Thread " + this.webSocketConnectionThread.Name + " started.");
            Console.WriteLine("Thread state at initialize thread end: " + this.webSocketConnectionThread.ThreadState);
        }

        /// <summary>
        /// Checks whether the websocket thread is still alive and whether it has passed exceptions.
        /// <exception>The websocket thread has passed an exception. The passed exception is thrown by this method.</exception>
        /// </summary>
        private void CheckoutWebSocket()
        {
            Console.WriteLine("Checkout web socket.");
            if (this.exceptionsFromWebSocketCommunicator.Count > 0)
            {
                if (!this.cancellationTokenSource.Token.IsCancellationRequested)
                    this.cancellationTokenSource.Cancel();
                Exception exception = this.exceptionsFromWebSocketCommunicator.Dequeue();
                Debug.WriteLine("An exception occurred in the websocket thread.");
                Console.WriteLine("An exception occurred in the websocket thread.");
                throw exception;
            }
            else if (this.webSocketConnectionThread == null | !this.webSocketConnectionThread.IsAlive)
            {
                Debug.WriteLine("Reinitializing websocket thread.");
                Console.WriteLine("Reinitializing websocket thread. Alive: " + this.webSocketConnectionThread.IsAlive);
                Console.WriteLine("Cancellation requested: " + this.cancellationTokenSource.Token.IsCancellationRequested);
                Console.WriteLine("Thread state: " + this.webSocketConnectionThread.ThreadState);
                this.InitializeWebSocketThread();
            }
        }

        /// <summary>
        /// Sends a stop signal to the thread running the websocket connection of this connector to close the connection and stop the thread.
        /// </summary>
        public void CloseWebSocketConnection()
        {
            this.cancellationTokenSource.Cancel();
        }


        /*********************************************
         * Message parsing
         ********************************************/

        /// <summary>
        /// Parses and stores a message received from the server, so it can be retrieved by another method later on.
        /// </summary>
        /// <param name="serverMessage">A message from the server that should be stored.</param>
        protected internal void StoreMessageFromServer(string serverMessage)
        {
            Console.WriteLine("Storing message from server: " + serverMessage);
            ServerMessage parsedMessage = ParseServerMessage(serverMessage);
            string action;
            if (parsedMessage == null)
                // Ignore useless messages.
                return;
            if (Actions.GetActions().Contains(parsedMessage.action))
                action = parsedMessage.action;
            else
                action = Actions.Default;
            this.InitializeReceivedMessagesActionForUser(action, parsedMessage.user_id);
            this.receivedMessages[action][parsedMessage.user_id].Add(parsedMessage);
        }

        /// <summary>
        /// Removes a given server message from the received messages dictionary.
        /// </summary>
        /// <param name="message">The server message to be removed from the received messages.</param>
        /// <list type="table">
        ///     <item>
        ///         <term>Post</term>
        ///         <description>
        ///         If the given <paramref name="message"/> was in the received message dictionary under the its <paramref name="action"/>, 
        ///         then it has been removed now.
        ///         </description>
        ///     </item>
        /// </list>
        private void RemoveReceivedMessage(ServerMessage message)
        {
            try
            {
                this.receivedMessages[message.action][message.user_id].Remove(message);
            } 
            catch(KeyNotFoundException e)
            {
                Console.WriteLine("Tried to remove message from received messages, but a key error occurred: " + e);
            }
        }

        /// <summary>
        /// Creates a server message set in the received message dictionary at a key equal to the value of the given <paramref name="action"/>
        /// for the user identified by the given <paramref name="userID"/>.
        /// </summary>
        /// <param name="action">The key of the dictionary where a server message set must be initialized. The value of this action must be in 
        /// the value collection provided by <c>Actions.GetActions()</c>.</param>
        /// <param name="userID">The ID of the user for whom a set must be initialized.</param>
        /// <list type="table">
        ///     <item>
        ///         <term>Post</term>
        ///         <description>
        ///         If the given <paramref name="userID"/> wasn't in the message list under the given <paramref name="action"/>, then
        ///         a new server message set is added under <paramref name="action"/> at a key equal to the given <paramref name="userID"/>.
        ///         Else the received message dictionary has been left unchanged.
        ///         </description>
        ///     </item>
        /// </list>
        private void InitializeReceivedMessagesActionForUser(string action, string userID)
        {
            if (!this.receivedMessages[action].ContainsKey(userID))
                this.receivedMessages[action].Add(userID, new HashSet<ServerMessage>());
        }

        /// <summary>
        /// Processes a json string received from the server and returns a server message object.
        /// </summary>
        /// <param name="serverMessage">The message from the server as a json string.</param>
        /// <returns>A server message object containing the information of the given 
        /// <paramref name="serverMessage" /> as far as the structure allows it.
        /// <c>null</c> in case the given message couldn't be parsed.</returns>
        private static ServerMessage ParseServerMessage(string serverMessage)
        {
            try
            {
                // check which type of server message
                ServerMessage message = JsonSerializer.Deserialize<ServerMessage>(serverMessage);
                // deserialise to specific type: ServerAnswer, ServerQuestionsMessage ...
                switch (message.action)
                {
                    case Actions.Answer:
                        message = JsonSerializer.Deserialize<ServerAnswer>(serverMessage);
                        break;
                    case Actions.Questions:
                        message = JsonSerializer.Deserialize<ServerQuestionsMessage>(serverMessage);
                        break;
                    default:
                        break;
                }
                return message;
            } 
            catch(JsonException)
            {
                Console.WriteLine("Message not in Json format: " + serverMessage);
                return null;
            }
        }

        /// <summary>
        /// Processes a user message received from the chatbot and returns a json string that complies 
        /// to structure that can be understood by the server.
        /// </summary>
        /// <param name="chatbotRequest">The request from the chatbot as a user message object.</param>
        /// <returns>A json string that complies to the structure understood by the server containing the information of the given 
        /// <paramref name="chatbotRequest" /> as far as the structure allows it.</returns>
        private static string ParseChatbotRequest(UserMessage chatbotRequest)
        {
            return JsonSerializer.Serialize(chatbotRequest);
        }

        /// <summary>
        /// Parses a user message from the chatbot and adds it to the send queue.
        /// </summary>
        /// <param name="chatbotRequest">A message from the chatbot to be sent to the server.</param>
        /// <exception cref="Exception">An exception has been passed by the web socket thread.</exception>
        private void AddMessageToSendQueue(UserMessage chatbotRequest)
        {
            Console.WriteLine("Adding message to send queue: " + chatbotRequest);
            this.CheckoutWebSocket();
            string message = ParseChatbotRequest(chatbotRequest);
            this.messagesToBeSent.Enqueue(message);
        }


        /*********************************************
         * Questions from user to server
         ********************************************/

        /// <summary>
        /// A temporarily ID assigned to questions asked by users, so the answer from the server can be identified.
        /// </summary>
        private int tempChatbotID = 0;

        /// <summary>
        /// Updates the <c>tempChatbotID</c> modulo 5000 and returns the updated value.
        /// </summary>
        private int GetNextTempChatbotID()
        {
            this.tempChatbotID = (tempChatbotID + 1) % 5000;
            return this.tempChatbotID;
        }

        /// <summary>
        /// Sends a given question from a given user to the server and returns the answer from the server.
        /// </summary>
        /// <param name="userID">The ID of the user for whom an answer is required.</param>
        /// <param name="question">The question to which an answer is required.</param>
        /// <param name="timeout">The timeout to be set in seconds before throwing an exception.</param>
        /// <returns>A server answer object with a question ID assigned to the given question by the server.
        /// In case the <c>answer</c> property of the returned server answer is <c>null</c>, then the server the server has assigned a 
        /// question ID to the given question, but it hasn't found an answer yet.</returns>
        /// <exception cref="TimeoutException">A timeout occurred and no response has been received from the server to this question, 
        /// so no question ID could be assigned to the given question. Try again later or use a higher timeout to avoid this.</exception>
        public async Task<ServerAnswer> SendQuestion(string userID, string question, double timeout=5)
        {
            Console.WriteLine("Send question method called.");
            UserQuestion request = new UserQuestion
            {
                user_id = userID,
                question = question,
                chatbot_temp_id = this.GetNextTempChatbotID()
            };
            this.AddMessageToSendQueue(request);
                var answer = await Task.Run(() => this.GetAnswerFromServerToQuestion(request.chatbot_temp_id, userID, timeout));
            if (answer == null)
                throw new TimeoutException("No response was received from the server to this question, so no question ID could be assigned. " +
                    "Try again later or use a higher timeout.");
            return answer;
        }

        /// <summary>
        /// Waits until <paramref name="timeout"/> for an answer from the server to the question identified by the given <paramref name="tempChatbotID"/> and asked
        /// by the user identified by the given <paramref name="userID"/>.
        /// </summary>
        /// <param name="tempChatbotID"></param>
        /// <param name="userID">The ID of the user for whom an answer is required.</param>
        /// <param name="timeout">The timeout to be set in seconds before throwing an exception.</param>
        /// <returns>A server answer object with a question ID assigned to the given question by the server.</returns>
        /// <exception cref="TimeoutException">A timeout occurred and no response has been received from the server 
        /// to this question, so no question ID could be assigned to the given question. Try again later or use a higher timeout to avoid this.</exception>
        private ServerAnswer GetAnswerFromServerToQuestion(int tempChatbotID, string userID, double timeout)
        {
            Console.WriteLine("Waiting for answer from server.");
            // set timeout and wait for answer
            // convert timeout to milliseconds
            timeout *= 1000;
            ServerAnswer answer = this.GetAnswerToQuestionOfUserByTempChatbotID(tempChatbotID, userID);
            bool found = answer != null;
            var watch = Stopwatch.StartNew();
            double elapsedMs = 0;
            while (!found & !(elapsedMs > timeout))
            {
                elapsedMs = watch.ElapsedMilliseconds;
                answer = this.GetAnswerToQuestionOfUserByTempChatbotID(tempChatbotID, userID);
                found = answer != null;
            }
            watch.Stop();
            Console.WriteLine("Found anwser: " + answer);
            if (!found)
                return null;
            return answer;
        }

        /// <summary>
        /// Returns all answers received from the server.
        /// </summary>
        /// <returns>A set containing all answers received from the serverin response to questions from this client.</returns>
        public ISet<ServerAnswer> GetNewResponses()
        {
            return (ISet<ServerAnswer>) this.receivedMessages[Actions.Answer].SelectMany(d => d.Value);
        }

        /// <summary>
        /// Returns all answers received from the server and addressed to the user identified by the given <paramref name="userID"/>.
        /// </summary>
        /// <param name="userID">The user ID of the user who wants to receive answers to previously asked questions.</param>
        /// <returns>A set containing all answers received from the server and addressed to the user identified by the given <paramref name="userID"/>.</returns>
        public ISet<ServerAnswer> GetNewAnswersForUser(string userID)
        {
            return new HashSet<ServerAnswer>((ISet<ServerAnswer>) this.receivedMessages[Actions.Answer][userID]);
        }

        /// <summary>
        /// Checks whether the server has an answer to the question of the given user, identified by its <paramref name="questionID"/> 
        /// and <paramref name="userID"/>.
        /// </summary>
        /// <param name="questionID">The question ID of the question for which is checked whether an answer is available.</param>
        /// <param name="userID">The user ID of the user for whom it is checked whether an answer is available.</param>
        /// <returns>True if and only if there is a server answer for the user identified with the given <paramref name="userID"/> 
        /// among the received messages which has the given <paramref name="questionID"/> as its question ID.</returns>
        public bool HasAnswerToQuestionOfUser(int questionID, string userID)
        {
            foreach (ServerMessage answer in this.receivedMessages[Actions.Answer][userID])
                try
                {
                    if (((ServerAnswer)answer).question_id == questionID)
                        return true;
                } 
                catch(InvalidCastException)
                {
                    Debug.WriteLine("Illegal server message added to server answers for user with id " + userID + 
                        " when looking for question with ID " + questionID + ": " + answer);
                }
            return false;
        }

        /// <summary>
        /// Checks whether the server has an answer to the question of the given user, identified by its <paramref name="chatbotTempID"/> 
        /// and <paramref name="userID"/>.
        /// </summary>
        /// <param name="chatbotTempID">The <c>chatbot_temp_id</c> of the question for which is checked whether an answer is available.</param>
        /// <param name="userID">The user ID of the user for whom it is checked whether an answer is available.</param>
        /// <returns>An answer if and only if there is a server answer for the user identified with the given <paramref name="userID"/> 
        /// among the received messages which has the given <paramref name="chatbotTempID"/> as its <c>chatbot_temp_id</c>.
        /// Else, null is returned.</returns>
        private ServerAnswer GetAnswerToQuestionOfUserByTempChatbotID(int chatbotTempID, string userID)
        {
            try
            {
                foreach (ServerMessage answer in this.receivedMessages[Actions.Answer][userID])
                    try
                    {
                        if (((ServerAnswer)answer).chatbot_temp_id == chatbotTempID)
                            return (ServerAnswer) answer;
                    }
                    catch (InvalidCastException)
                    {
                        Debug.WriteLine("Illegal server message added to server answers for user with id " + userID +
                            " when looking for question with ChatbotTempID " + chatbotTempID + ": " + answer);
                        Console.WriteLine("Illegal server message added to server answers for user with id " + userID +
                            " when looking for question with ChatbotTempID " + chatbotTempID + ": " + answer);
                    }
            }
            catch(KeyNotFoundException)
            {
                // userID not in message dictionary under answer key.
            }
            return null;
        }


        /*********************************************
         * Questions for user from server
         ********************************************/

        /// <summary>
        /// Returns all available questions that should be answered.
        /// </summary>
        /// <returns>A set containing questions that should be answered.</returns>
        public List<ServerMessage> GetQuestionsToBeAnswered()
        {
            return this.receivedMessages[Actions.Questions].SelectMany(d => d.Value).ToList();
        }

        /// <summary>
        /// Returns all available questions addressed to the user identified by the given <paramref name="userID"/>.
        /// </summary>
        /// <param name="userID">The user ID of the user to whom the returned questions should be addressed.</param>
        /// <returns>A set containing questions addressed to the user identified by the given <paramref name="userID"/>.
        ///          'null' in case no questions messages were found.</returns>
        public ISet<ServerQuestionsMessage> GetQuestionsAddressedToUser(string userID)
        {
            try
            {
                return new HashSet<ServerQuestionsMessage>((ISet<ServerQuestionsMessage>)this.receivedMessages[Actions.Questions][userID]);
            }
            catch(KeyNotFoundException)
            {
                return null;
            }
        }

        /// <summary>
        /// Creates a request to the server to receive unanswered questions for a user.
        /// </summary>
        /// <param name="userID">The user ID of the user who should answer the questions.</param>
        /// <returns>A set of server questions. If the set is empty, no questions are available.</returns>
        /// <exception cref="Exception">The websocket thread has passed an exception. The passed exception is thrown by this method.</exception>
        public async Task<ISet<ServerQuestion>> RequestUnansweredQuestionsAsync(string userID, double timeout = 5)
        {
            Console.WriteLine("Request questions method called.");
            this.CheckoutWebSocket();
            // Create set of questions
            ISet<ServerQuestion> questions = new HashSet<ServerQuestion>();
            // Check if questions offline -> probably not, but if there are any, add them
            ISet<ServerQuestionsMessage> storedQuestionsMessages = this.GetQuestionsAddressedToUser(userID);
            if (storedQuestionsMessages.Count > 0)
                foreach (ServerQuestionsMessage questionsMessage in storedQuestionsMessages)
                {
                    if (questionsMessage.answer_questions.Count > 0)
                        foreach (ServerQuestion question in questionsMessage.answer_questions)
                            questions.Add(question);

                }
            else
            {
                // Create request for server
                UserRequest request = new UserRequest
                {
                    user_id = userID,
                    request = Requests.UnansweredQuestions
                };
                this.AddMessageToSendQueue(request);

                // Wait until questions received or timeout
                var answer = await Task.Run(() => this.GetResponseFromServerToRequest(userID, Actions.Questions, timeout));
                
                if (answer.Count > 0)
                {
                    ISet<ServerQuestionsMessage> questionsMessages = (ISet<ServerQuestionsMessage>)answer;
                    // Fill set of questions
                    foreach (ServerQuestionsMessage questionsMessage in questionsMessages)
                    {
                        if (questionsMessage.answer_questions.Count > 0)
                            foreach (ServerQuestion question in questionsMessage.answer_questions)
                                questions.Add(question);
                        // Remove message from cache
                        this.RemoveReceivedMessage(questionsMessage);
                    }

                }
            }
            return questions;
        }

        /// <summary>
        /// Waits until <paramref name="timeout"/> for an answer from the server to the question identified by the given <paramref name="tempChatbotID"/> and asked
        /// by the user identified by the given <paramref name="userID"/>.
        /// </summary>
        /// <param name="tempChatbotID"></param>
        /// <param name="userID">The ID of the user for whom an answer is required.</param>
        /// <param name="timeout">The timeout to be set in seconds before throwing an exception.</param>
        /// <returns>A set of server questions message objects.
        ///          'null' in case no response was received before timeout.</returns>
        private ISet<ServerMessage> GetResponseFromServerToRequest(string userID, string expectedResponseAction, double timeout)
        {
            Console.WriteLine("Waiting for questions from server.");
            // set timeout and wait for answer
            // convert timeout to milliseconds
            timeout *= 1000;
            bool found = false;
            ISet<ServerMessage> answer = null;
            try
            {
                answer = this.receivedMessages[expectedResponseAction][userID];
                found = answer.Count > 0;
            }
            catch(KeyNotFoundException)
            {
                var watch = Stopwatch.StartNew();
                double elapsedMs = 0;
                while (!found & !(elapsedMs > timeout))
                {
                    elapsedMs = watch.ElapsedMilliseconds;
                    try
                    {
                        answer = this.receivedMessages[expectedResponseAction][userID];
                        found = answer != null;
                    } 
                    catch(KeyNotFoundException)
                    {
                        answer = null;
                    }
                }
                watch.Stop();
                Console.WriteLine("Found response to request: " + answer);
                if (!found)
                    return null;
            }
            
            return answer;
        }


        /*********************************************
         * Answers from user to server questions
         ********************************************/

        /// <summary>
        /// Sends an <paramref name="answer"/> provided by a user identified by the given <paramref name="userID"/> to a question identified by
        /// the given <paramref name="questionID"/>.
        /// </summary>
        /// <param name="userID">The user ID identifying the user who submitted the answer.</param>
        /// <param name="questionID">The question ID identifying the question for which an <paramref name="answer"/> is given.</param>
        /// <param name="answer">An answer to the question identified by the given <paramref name="questionID"/>.</param>
        /// <exception cref="Exception">An exception has been passed by the web socket thread.</exception>
        public void AnswerQuestion(string userID, int questionID, string answer)
        {
            Console.WriteLine("Answer question method called.");
            UserAnswer userAnswer = new UserAnswer
            {
                question_id = questionID,
                answer = answer
            };
            this.AnswerQuestion(userID, userAnswer);
        }

        /// <summary>
        /// Sends an <paramref name="answer"/> provided by a user identified by the given <paramref name="userID"/>.
        /// </summary>
        /// <param name="userID">The user ID identifying the user who submitted the answer.</param>
        /// <param name="answer">The answer to be sent.</param>
        /// <exception cref="Exception">An exception has been passed by the web socket thread.</exception>
        public void AnswerQuestion(string userID, UserAnswer answer)
        {
            Console.WriteLine("Answer question method UserAnswer called.");
            UserAnswersMessage answers = new UserAnswersMessage
            {
                user_id = userID
            };
            answers.AddAnswer(answer);
            this.AddMessageToSendQueue(answers);
        }

        /// <summary>
        /// Processes a series of questionID-answer pairs from a user identified by the given <paramref name="userID"/>.
        /// </summary>
        /// <param name="userID">The user id of the user who wants to send answers to questions.</param>
        /// <param name="questionIDAnswerPairs">A collection containing pairs consisting of a question ID and the answer
        /// provided by the user to the question belonging to the given question ID.</param>
        /// <list type="table">
        ///     <item>
        ///         <term>Pre</term>
        ///         <description>Every question ID only occurs once in the given collection.</description>
        ///     </item>
        /// </list>
        /// <exception cref="Exception">An exception has been passed by the web socket thread.</exception>
        public void AnswerQuestions(string userID, ICollection<Tuple<int, string>> questionIDAnswerPairs)
        {
            UserAnswersMessage answers = new UserAnswersMessage
            {
                user_id = userID
            };
            foreach(Tuple<int, string> questionIDAnswerPair in questionIDAnswerPairs)
            {
                int questionID = questionIDAnswerPair.Item1;
                string answer = questionIDAnswerPair.Item2;
                UserAnswer userAnswer = new UserAnswer
                {
                    question_id = questionID,
                    answer = answer
                };
                answers.AddAnswer(userAnswer);
            }
            this.AddMessageToSendQueue(answers);
        }

        /// <summary>
        /// Processes a series of questionID-answer pairs from a user identified by the given <paramref name="userID"/>.
        /// </summary>
        /// <param name="userID">The user id of the user who wants to send answers to questions.</param>
        /// <param name="questionIDAnswerPairs">A collection containing pairs consisting of a question ID and the answer
        /// provided by the user to the question belonging to the given question ID.</param>
        /// <list type="table">
        ///     <item>
        ///         <term>Pre</term>
        ///         <description>Every answer in the given collection has a unique question ID among the answers in that collection.</description>
        ///     </item>
        /// </list>
        /// <exception cref="Exception">An exception has been passed by the web socket thread.</exception>
        public void AnswerQuestions(string userID, ICollection<UserAnswer> userAnswers)
        {
            UserAnswersMessage answers = new UserAnswersMessage
            {
                user_id = userID
            };
            answers.AddAnswers(userAnswers);
            this.AddMessageToSendQueue(answers);
        }


        /*********************************************
         * Feedback
         ********************************************/

        /// <summary>
        /// Processes a feedback response from a user identified by the given <paramref name="userID"/> concerning 
        /// the question-answer pair identified by <paramref name="questionID"/> and <paramref name="answerID"/>.
        /// </summary>
        /// <param name="userID">The user id of the user who wants to send feedback.</param>
        /// <param name="answerID">The answer ID related to the question-answer pair for which feedback is sent.</param>
        /// <param name="questionID">The question ID related to the question-answer pair for which feedback is sent.</param>
        /// <param name="feedback">A feedback code related to the feedback. This could be as simple as 'good' = 1 and 'bad' = 0, 
        /// or more advanced using feelings like 'happy' = 0, 'angry' = 1, 'sad' = 2 ...  as long as the server understands it well.</param>
        /// <exception cref="Exception">An exception has been passed by the web socket thread.</exception>
        public void SendFeedbackOnAnswer(string userID, int answerID, int questionID, int feedback)
        {
            Console.WriteLine("Send feedback method called.");
            UserFeedback userFeedback = new UserFeedback
            {
                user_id = userID,
                answer_id = answerID,
                question_id = questionID,
                feedback_code = feedback
            };
            this.AddMessageToSendQueue(userFeedback);
        }
    }
}
