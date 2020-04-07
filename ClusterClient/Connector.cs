using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
        public Connector(string webSocketHostURI = "wss://clusterapi20200320113808.azurewebsites.net/api/bot/WS", int webSocketConnectionTimeout = 10)
        {
            this.webSocketHostURI = new Uri(webSocketHostURI);
            this.webSocketConnectionTimeout = webSocketConnectionTimeout;
            this.cancellationTokenSource = new CancellationTokenSource();
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
        ///         <description>The set belonging to a certain Action can only contain instances of the ServerMessage subclass related to that
        ///         Action. E.g. Questions can contain instances of ServerQuestionsMessage, Answer of ServerAnswer.</description>
        ///     </item>
        /// </list>
        private readonly IDictionary<string, Dictionary<int, ISet<ServerMessage>>> receivedMessages = new Dictionary<string, Dictionary<int, ISet<ServerMessage>>>()
            {
                { Actions.Default, new Dictionary<int, ISet<ServerMessage>>() },
                { Actions.Questions, new Dictionary<int, ISet<ServerMessage>>() },
                { Actions.Answer, new Dictionary<int, ISet<ServerMessage>>() }
            };

        /// <summary>
        /// Variable referencing a queue in which exceptions thrown by the websocket thread are passed to this <c>Connector</c> instance.
        /// </summary>
        private readonly Queue<Exception> exceptionsFromWebSocketCommunicator = new Queue<Exception>();

        /// <summary>
        /// Variable referencing a cancelation token source used to control tasks.
        /// </summary>
        private readonly CancellationTokenSource cancellationTokenSource;

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
                this.cancellationTokenSource.Cancel();
                // Might not be necessary.
                this.webSocketCommunicator.Stop = true;
            }
            Debug.WriteLine("Clearing exception queue.");
            this.exceptionsFromWebSocketCommunicator.Clear();
            Debug.WriteLine("Starting new thread.");
            this.webSocketCommunicator = new WebSocketCommunicator(this.webSocketHostURI, this.exceptionsFromWebSocketCommunicator, 
                                                        this.StoreMessageFromServer, this.messagesToBeSent, this.webSocketConnectionTimeout, this.cancellationTokenSource.Token);
            this.webSocketConnectionThread = new Thread(new ThreadStart(this.webSocketCommunicator.Run));
            this.webSocketConnectionThread.Start();
            Debug.WriteLine("Thread " + this.webSocketConnectionThread.Name + " started.");
        }

        /// <summary>
        /// Checks whether the websocket thread is still alive and whether it has passed exceptions.
        /// <exception>The websocket thread has passed an exception. The passed exception is thrown by this method.</exception>
        /// </summary>
        private void CheckoutWebSocket()
        {
            if (this.exceptionsFromWebSocketCommunicator.Count > 0)
            {
                if (!this.cancellationTokenSource.Token.IsCancellationRequested)
                    this.cancellationTokenSource.Cancel();
                Exception exception = this.exceptionsFromWebSocketCommunicator.Dequeue();
                Debug.WriteLine("An exception occurred in the websocket thread.");
                throw exception;
            }
            else if (this.webSocketConnectionThread == null | !this.webSocketConnectionThread.IsAlive)
            {
                Debug.WriteLine("Reinitializing websocket thread.");
                this.InitializeWebSocketThread();
            }
        }

        /// <summary>
        /// Sends a stop signal to the thread running the websocket connection of this connector to close the connection and stop the thread.
        /// </summary>
        public void CloseWebSocketConnection()
        {
            this.cancellationTokenSource.Cancel();
            // Probabily not needed:
            this.webSocketCommunicator.Stop = true;
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
            ServerMessage parsedMessage = ParseServerMessage(serverMessage);
            string action;
            if (Actions.GetActions().Contains(parsedMessage.Action))
                action = parsedMessage.Action;
            else
                action = Actions.Default;
            this.InitializeReceivedMessagesActionForUser(action, parsedMessage.UserID);
            this.receivedMessages[action][parsedMessage.UserID].Add(parsedMessage);
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
        private void InitializeReceivedMessagesActionForUser(string action, int userID)
        {
            if (!this.receivedMessages[action].ContainsKey(userID))
                this.receivedMessages[action].Add(userID, new HashSet<ServerMessage>());
        }

        /// <summary>
        /// Processes a json string received from the server and returns a server message object.
        /// </summary>
        /// <param name="serverMessage">The message from the server as a json string.</param>
        /// <returns>A server message object containing the information of the given 
        /// <paramref name="serverMessage" /> as far as the structure allows it.</returns>
        private static ServerMessage ParseServerMessage(string serverMessage)
        {
            // check which type of server message
            ServerMessage message = JsonSerializer.Deserialize<ServerMessage>(serverMessage);
            // deserialise to specific type: ServerAnswer, ServerQuestionsMessage ...
            switch (message.Action)
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
            this.CheckoutWebSocket();
            string message = ParseChatbotRequest(chatbotRequest);
            this.messagesToBeSent.Enqueue(message);
        }


        /*********************************************
         * Questions from user to server
         ********************************************/

        /// <summary>
        /// Sends a given question from a given user to the server.
        /// </summary>
        /// <param name="userID">The ID of the user for whom an answer is required.</param>
        /// <param name="question">The question to which an answer is required.</param>
        /// <returns>A question ID assigned to this question by the server.</returns>
        public string SendQuestion(int userID, string question) // ForumQuestion() return questionID
        {
            // parse request -> message
            // add message to queue
            // set timeout and wait for QuestionID and/or answer
            // return QuestionID / answer?
            return "";
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
        public ISet<ServerAnswer> GetNewAnswersForUser(int userID)
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
        public bool HasAnswerToQuestionOfUser(int questionID, int userID)
        {
            foreach (ServerMessage answer in this.receivedMessages[Actions.Answer][userID])
                try
                {
                    if (((ServerAnswer)answer).QuestionID == questionID)
                        return true;
                } 
                catch(InvalidCastException)
                {
                    Debug.WriteLine("Illegal server message added to server answers for user with id " + userID + 
                        " when looking for question with ID " + questionID + ": " + answer);
                }
            return false;
        }


        /*********************************************
         * Questions for user from server
         ********************************************/

        /// <summary>
        /// Returns all available questions that should be answered.
        /// </summary>
        /// <returns>A set containing questions that should be answered.</returns>
        public ISet<ServerQuestionsMessage> GetQuestionsToBeAnswered()
        {
            return (ISet<ServerQuestionsMessage>) this.receivedMessages[Actions.Questions].SelectMany(d => d.Value);
        }

        /// <summary>
        /// Returns all available questions addressed to the user identified by the given <paramref name="userID"/>.
        /// </summary>
        /// <param name="userID">The user ID of the user to whom the returned questions should be addressed.</param>
        /// <returns>A set containing questions addressed to the user identified by the given <paramref name="userID"/>.</returns>
        public ISet<ServerQuestionsMessage> GetQuestionsAddressedToUser(int userID)
        {
            return new HashSet<ServerQuestionsMessage>((ISet<ServerQuestionsMessage>) this.receivedMessages[Actions.Questions][userID]);
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
        public void AnswerQuestion(int userID, int questionID, string answer)
        {
            UserAnswer userAnswer = new UserAnswer
            {
                QuestionID = questionID,
                Answer = answer
            };
            this.AnswerQuestion(userID, userAnswer);
        }

        /// <summary>
        /// Sends an <paramref name="answer"/> provided by a user identified by the given <paramref name="userID"/>.
        /// </summary>
        /// <param name="userID">The user ID identifying the user who submitted the answer.</param>
        /// <param name="answer">The answer to be sent.</param>
        /// <exception cref="Exception">An exception has been passed by the web socket thread.</exception>
        public void AnswerQuestion(int userID, UserAnswer answer)
        {
            UserAnswersMessage answers = new UserAnswersMessage
            {
                UserID = userID
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
        public void AnswerQuestions(int userID, ICollection<Tuple<int, string>> questionIDAnswerPairs)
        {
            UserAnswersMessage answers = new UserAnswersMessage
            {
                UserID = userID
            };
            foreach(Tuple<int, string> questionIDAnswerPair in questionIDAnswerPairs)
            {
                int questionID = questionIDAnswerPair.Item1;
                string answer = questionIDAnswerPair.Item2;
                UserAnswer userAnswer = new UserAnswer
                {
                    QuestionID = questionID,
                    Answer = answer
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
        public void AnswerQuestions(int userID, ICollection<UserAnswer> userAnswers)
        {
            UserAnswersMessage answers = new UserAnswersMessage
            {
                UserID = userID
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
        public void SendFeedbackOnAnswer(int userID, int answerID, int questionID, int feedback)
        {
            UserFeedback userFeedback = new UserFeedback
            {
                UserID = userID,
                AnswerID = answerID,
                QuestionID = questionID,
                FeedbackCode = feedback
            };
            this.AddMessageToSendQueue(userFeedback);
        }
    }
}
