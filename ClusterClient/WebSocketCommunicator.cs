using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClusterClient
{
    /// <summary>
    /// A class meant to be run in a separate thread and used to manage a websocket connection to communicate with a server.
    /// </summary>
    class WebSocketCommunicator
    {
        /// <summary>
        /// Initiates a thread to run a websocket connection, send messages from a <paramref name="sendQueue" /> and receive messages, saving them using a
        /// given <paramref name="passMessageToClient"/> method. Exceptions thrown by the methods of this thread are saved in a <paramref name="exceptionQueue"/>.
        /// </summary>
        /// <param name="webSocketURI">A URI containing the uri of the websocket host with which a connection should be made.</param>
        /// <param name="exceptionQueue">A reference to a queue in which thrown exceptions should be saved to be passed on to the caller of this thread.</param>
        /// <param name="passMessageToClient">An instance method of the calling class that handles received messages.</param>
        /// <param name="sendQueue">A queue in which the messages to be sent can be found.</param>
        /// <param name="connectionTimeout">The timeout to be set when connecting to the websocket host.</param>
        /// <param name="cancellationToken">The cancellation token to be used to stop this thread from running.</param>
        public WebSocketCommunicator(Uri webSocketURI, Queue<Exception> exceptionQueue, MethodToPassMessageToClient passMessageToClient,
            Queue<string> sendQueue, int connectionTimeout, CancellationToken cancellationToken)
        {
            this.Stop = false;
            this.webSocketURI = webSocketURI;
            this.exceptionQueue = exceptionQueue;
            this.PassMessageToClient = passMessageToClient;
            this.sendQueue = sendQueue;
            this.connectionTimeoutSeconds = connectionTimeout;
            this.cancellationToken = cancellationToken;
        }

        /// <summary>
        /// A boolean controlling the running state of this thread. When <c>Stop</c> is set to <c>true</c>, running tasks are interrupted, the 
        /// websocket is closed if it was open and the thread returns. When <c>Stop</c> is set to <c>true</c> by a method of this thread, 
        /// an exception is added to the <c>exceptionQueue</c> provided at initialization of this thread.
        /// </summary>
        public bool Stop { get; set; }

        /// <summary>
        /// A cancellation token controlling the running state of this thread. When it is cancelled, running tasks are interrupted, the 
        /// websocket is closed if it was open and the thread returns.
        /// </summary>
        private CancellationToken cancellationToken;

        /// <summary>
        /// A string containing the uri of the websocket host with which a connection should be made.
        /// </summary>
        private Uri webSocketURI;

        /// <summary>
        /// Variable referencing the websocket connection of this thread.
        /// </summary>
        private ClientWebSocket webSocket;

        /// <summary>
        /// A reference to a queue in which thrown exceptions should be saved to be passed on to the caller of this thread.
        /// </summary>
        private readonly Queue<Exception> exceptionQueue;

        /// <summary>
        /// A queue in which the messages to be sent can be found.
        /// </summary>
        private readonly Queue<string> sendQueue;

        /// <summary>
        /// The timeout to be set when connecting to the websocket host.
        /// </summary>
        private int connectionTimeoutSeconds;

        /// <summary>
        /// An instance method of the calling class that handles received messages.
        /// </summary>
        /// <param name="messageFromServer"></param>
        public delegate void MethodToPassMessageToClient(string messageFromServer);

        /// <summary>
        /// Variable referencing an instance method of the calling class that handles received messages.
        /// </summary>
        private readonly MethodToPassMessageToClient PassMessageToClient;

        /// <summary>
        /// Starts communication with the websocket host.
        /// </summary>
        public void Run()
        {
            this.CommunicateWithServerAsync();
        }

        /// <summary>
        /// Checks for new messages from server and processes them.
        /// Sets <c>this.Stop</c> to <c>true</c> when the websocket raises a ... exception.
        /// </summary>
        /// <returns>null if the websocket raises a ... exception.</returns>
        /// <list type="table">
        ///     <item>
        ///         <term>Post</term>
        ///         <description><c>this.Stop</c> equals <c>true</c></description>
        ///     </item>
        /// </list>
        private async Task ReceiveMessagesAsync()
        {
            while (!this.Stop)
            {
                this.cancellationToken.ThrowIfCancellationRequested();
                // Reserve 1 kB buffer to store received message.
                ArraySegment<byte> bytesReceived = new ArraySegment<byte>(new byte[1024]);
                WebSocketReceiveResult result = await this.webSocket.ReceiveAsync(
                            bytesReceived, this.cancellationToken);
                try
                {
                    string message = Encoding.UTF8.GetString(bytesReceived.Array, 0, result.Count);
                    this.ProcessReceivedMessage(message);
                }
                catch (ArgumentNullException)
                {
                    Debug.WriteLine("Websocket returned null message.");
                }   
                catch (ArgumentException)
                {
                    Debug.WriteLine("Received message contains invalid unicode code points and will be ignored.");
                }
                
            }
        }

        /// <summary>
        /// Passes received messages to the calling class instance.
        /// </summary>
        /// <param name="message">The message to be passed on to the calling class instance.</param>
        /// <returns></returns>
        private void ProcessReceivedMessage(string message)
        {
            this.PassMessageToClient(message);
        }

        /// <summary>
        /// Generates next message to be sent and removes it from the reply queue.
        /// </summary>
        /// <returns>A message to send to the server.</returns>
        private IEnumerable<string> MessagesToSend
        {
            get
            {
                // The lock in the next block is not necessary in case the queue has been wrapped in a
                // synchronized wrapper to make it thread safe, e.g. using <c>Queue mySyncdQ = Queue.Synchronized( myQ );</c>
                while (this.sendQueue.Count > 0)
                    lock (this.sendQueue)
                    {
                        yield return this.sendQueue.Dequeue();
                    }
                yield break;
            }
        }

        /// <summary>
        /// Sends messages from send queue. 
        /// Waits 10 millliseconds if no replies are available.
        /// </summary>
        /// <returns></returns>
        private async Task SendMessagesAsync()
        {
            while (!this.Stop)
            {
                this.cancellationToken.ThrowIfCancellationRequested();
                foreach(string message in this.MessagesToSend)
                {
                    ArraySegment<byte> bytesToSend = new ArraySegment<byte>(
                            Encoding.UTF8.GetBytes(message));
                    await this.webSocket.SendAsync(bytesToSend, WebSocketMessageType.Text, true, this.cancellationToken);
                    // Now what if message could not be sent? The WebSocket docs don't mention any exceptions thrown by SendAsync().
                    // However, in case the message wasn't sent due to some exception or the cancellation token being cancelled, 
                    // we cannot confirm it wasn't sent and the message will be lost.
                }
                await Task.Delay(10, this.cancellationToken);
            }
        }

        /// <summary>
        /// Lets sender and receiver handlers work asynchronously.
        /// </summary>
        /// <returns></returns>
        private async Task HandleSendReceiveTasksAsync()
        {
            var sendTask = this.SendMessagesAsync();
            var receiveTask = this.ReceiveMessagesAsync();

            var allTasks = new List<Task> { sendTask, receiveTask };

            await Task.WhenAny(allTasks);
            /*In case of resource issues, we could try to dispose the tasks, given they must be finished by now, 
            because they can only return when the cancellation token is cancelled.*/
            /*foreach (var task in allTasks)
                task.Dispose();*/
        }

        /// <summary>
        /// A default message to be sent to the server when the connection is established.
        /// </summary>
        private static readonly ArraySegment<byte> connectionEstablishedMessage = new ArraySegment<byte>(
                            Encoding.UTF8.GetBytes("{\"msg\": \"Connection established.\"}"));

        /// <summary>
        /// Keeps websocket connection running.
        /// Tries to connect to the websocket server. When the connection 
        /// has been established a 'Connection established' message is sent to the host. 
        /// If a ... exception is thrown for the first time, the method waits 1.5s asynchronously and 
        /// retries once afterwards. <c>this.Stop</c> is set to <c>true</c> when ... exception occurs a 
        /// second time or when another <c>Exception</c> is thrown.
        /// </summary>
        /// <returns><c>null</c> when <c>this.Stop</c> equals <c>true</c>.</returns>
        /// <list type="table">
        ///     <item>
        ///         <term>Post</term>
        ///         <description>Thrown <c>Exception</c>s are in <c>this.exceptionQueue</c>.</description>
        ///     </item>
        /// </list>
        private async Task CommunicateWithServerAsync()
        {
            using (this.webSocket = new ClientWebSocket())
            {
                try
                {
                    while (!this.Stop)
                    {
                        this.cancellationToken.ThrowIfCancellationRequested();
                        if (this.webSocket == null | this.webSocket.State != WebSocketState.Open)
                        {
                            Debug.WriteLine("Websocket NOT connected. Trying to connect. " + this.connectionTimeoutSeconds + "s timeout set.");
                            await this.ConnectToServerAsync();
                            await this.webSocket.SendAsync(connectionEstablishedMessage, WebSocketMessageType.Text, true, this.cancellationToken);
                        }
                        await this.HandleSendReceiveTasksAsync();
                    }
                } catch(Exception e)
                {
                    // Add the exception to the queue if it was not caused by the calling class instance.
                    if (!(e is OperationCanceledException))
                        this.exceptionQueue.Enqueue(e);
                }
                finally
                {
                    // Probably unnecessary
                    this.Stop = true;
                    //if (this.websocket != null & this.websocket.State == WebSocketState.Open)
                    // close websocket, now handled by 'using'
                    Debug.WriteLine("Communication with server ended.");
                }
            }
        }

        /// <summary>
        /// Creates a websocket connection using the uri of this websocket thread and a timeout set to <c>this.connectionTimeoutSeconds</c>*1000 milliseconds.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception">Something went wrong while trying to connect to the websocket host.</exception>
        private async Task ConnectToServerAsync()
        {
            CancellationTokenSource tempCancellationSource = new CancellationTokenSource(this.connectionTimeoutSeconds*1000);
            await this.webSocket.ConnectAsync(this.webSocketURI, tempCancellationSource.Token);
        }


    }
}
