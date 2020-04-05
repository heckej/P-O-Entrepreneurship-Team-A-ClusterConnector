using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace ClusterClient
{
    /// <summary>
    /// The class <c>Connector</c> allows communication with the Cluster API server by retrieving and sending questions and answers.
    /// </summary>
    public class Connector
    {
        /// <summary>
        /// Initializes a new connector instance with the given websocket host URI used for its websocket connection and the given timeout set as the timeout
        /// before giving up on trying to connect to the server.
        /// </summary>
        /// <param name="webSocketHostURI">The URI referencing the server address to which a websocket connection should be made.</param>
        /// <param name="webSocketConnectionTimeout">The timeout to be set in seconds for the websocket connection before giving up. By default set to 10 seconds.</param>
        public Connector(string webSocketHostURI= "wss://clusterapi20200320113808.azurewebsites.net/api/bot/WS", int webSocketConnectionTimeout = 10)
        {
            this.webSocketHostURI = new Uri(webSocketHostURI);
            this.webSocketConnectionTimeout = webSocketConnectionTimeout;
            this.cancellationTokenSource = new CancellationTokenSource();
        }

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
        /// Variable referencing the list in which messages from the server are stored until they are read
        /// </summary>
        private readonly List<string> receivedMessages = new List<string>();

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

        /// <summary>
        /// Parses and stores a message received from the server, so it can be retrieved by another method later on.
        /// </summary>
        /// <param name="serverMessage">A message from the server that should be stored.</param>
        protected internal void StoreMessageFromServer(string serverMessage)
        {

        }

        /// <summary>
        /// Processes a dictionary or a list of dictionaries received from the server and returns a list of dictionaries 
        /// that comply to the structure of the result of ...            
        /// </summary>
        /// <param name="serverMessage">The message from the server as a ... or a list of ....</param>
        /// <returns>A list of ... that comply to the structure of the result of ... containing the
        /// information of the given <paramref name="serverMessage" /> as far as the structure allows it.</returns>
        private static string ParseServerMessage(string serverMessage)
        {
            return "";
        }

        /// <summary>
        /// Processes a ... received from the chatbot and returns a ... that complies to structure that can be understood by the server.
        /// </summary>
        /// <param name="chatbotRequest">The request from the chatbot as a ...</param>
        /// <returns>A dictionary that complies to the structure understood by the server containing the information of the given <paramref name="chatbotRequest" /> 
        /// as far as the structure allows it.</returns>
        private static string ParseChatbotRequest(string chatbotRequest)
        {
            return "";
        }

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
        /// <returns>A set containing all answers received from the server.</returns>
        public ISet<string> GetNewResponses()
        {
            return new HashSet<string>();
        }

        /// <summary>
        /// Returns all answers received from the server and addressed to the user identified by the given <paramref name="userID"/>.
        /// </summary>
        /// <param name="userID">The user ID of the user who wants to receive answers to previously asked questions.</param>
        /// <returns>A set containing all answers received from the server and addressed to the user identified by the given <paramref name="userID"/>.</returns>
        public ISet<string> GetNewAnswersForUser(int userID)
        {
            return new HashSet<string>();
        }

        /// <summary>
        /// Checks whether the server has an answer to the question of the given user, identified by its <paramref name="questionID"/> and <paramref name="userID"/>.
        /// </summary>
        /// <param name="questionID">The question ID of the question for which is checked whether an answer is available.</param>
        /// <param name="userID">The user ID of the user for whom it is checked whether an answer is available.</param>
        /// <returns></returns>
        public bool HasAnswerToQuestionOfUser(int questionID, int userID)
        {
            return false;
        }

        /// <summary>
        /// Returns all available questions that should be answered.
        /// </summary>
        /// <returns>A set containing questions that should be answered.</returns>
        public ISet<string> GetQuestionsToBeAnswered()
        {
            return new HashSet<string>();
        }

        /// <summary>
        /// Returns all available questions addressed to the user identified by the given <paramref name="userID"/>.
        /// </summary>
        /// <param name="userID">The user ID of the user to whom the returned questions should be addressed.</param>
        /// <returns>A set containing questions addressed to the user identified by the given <paramref name="userID"/>.</returns>
        public ISet<string> GetQuestionsAddressedToUser(int userID)
        {
            // look up user in message 
            return new HashSet<string>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="questionID"></param>
        public void AnswerQuestion(int userID, int questionID)
        {
            // design message
            // add message to send queue
        }

        /// <summary>
        /// Handles a feedback response from a user identified by the given <paramref name="userID"/> concerning 
        /// the question-answer pair identified by <paramref name="questionID"/> and <paramref name="answerID"/>.
        /// </summary>
        /// <param name="userID">The user id of the user who wants to send feedback.</param>
        /// <param name="answerID">The answer ID related to the question-answer pair for which feedback is sent.</param>
        /// <param name="questionID">The question ID related to the question-answer pair for which feedback is sent.</param>
        /// <param name="feedback">A feedback code related to the feedback. This could be as simple as 'good' = 1 and 'bad' = 0, 
        /// or more advanced using feelings like 'happy' = 0, 'angry' = 1, 'sad' = 2 ...  as long as the server understands it well.</param>
        /// <returns></returns>
        public void SendFeedbackOnAnswer(int userID, int answerID, int questionID, int feedback)
        {
            // design message
            // add message to send queue
        }
    }


    /*class UserConnector : Connector
    {
    }*/



}
