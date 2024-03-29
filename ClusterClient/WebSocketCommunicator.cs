using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClusterClient.Models;

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
        public WebSocketCommunicator(Uri webSocketURI, Queue<Exception> exceptionQueue, Func<string, Task> passMessageToClient,
            Queue<string> sendQueue, int connectionTimeout, CancellationToken cancellationToken, string authorization)
        {
            this.webSocketURI = webSocketURI;
            this.exceptionQueue = exceptionQueue;
            this.PassMessageToClientAsync = passMessageToClient;
            this.sendQueue = sendQueue;
            this.connectionTimeoutSeconds = connectionTimeout;
            this.cancellationToken = cancellationToken;
            this.authorization = authorization;
        }

        /// <summary>
        /// A cancellation token controlling the running state of this thread. When it is cancelled, running tasks are interrupted, the 
        /// websocket is closed if it was open and the thread returns.
        /// </summary>
        private CancellationToken cancellationToken;

        /// <summary>
        /// The uri of the websocket host with which a connection should be made.
        /// </summary>
        private Uri webSocketURI;

        /// <summary>
        /// The value to be set for the Authorization header in the initial web socket connection request.
        /// </summary>
        private readonly string authorization;

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
        /// Variable referencing an instance method of the calling class that handles received messages.
        /// </summary>
        private readonly Func<string, Task> PassMessageToClientAsync;

        /// <summary>
        /// A boolean enabling a constant check on websocket state.
        /// </summary>
        public bool enableCheckWebSocketStateDebugging = false;

        /// <summary>
        /// Starts communication with the websocket host.
        /// </summary>
        public void Run()
        {
            Task task = this.CommunicateWithServerAsync();
            task.Wait();
        }

        /// <summary>
        /// Checks for new messages from server and processes them.
        /// </summary>
        /// <returns>null if the websocket raises a ... exception.</returns>
        private async Task ReceiveMessagesAsync()
        {
            while (true)
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
                catch(Exception e)
                {
                    Debug.WriteLine("Unexpected exception while receiving task: " + e);
                }
                
            }
        }

        /// <summary>
        /// Passes received messages to the calling class instance.
        /// </summary>
        /// <param name="message">The message to be passed on to the calling class instance.</param>
        /// <returns></returns>
        private async void ProcessReceivedMessage(string message)
        {
            Console.WriteLine("Processing received message: " + message);
            await this.PassMessageToClientAsync(message);
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
            while (true)
            {
                this.cancellationToken.ThrowIfCancellationRequested();
                foreach(string message in this.MessagesToSend)
                {
                    ArraySegment<byte> bytesToSend = new ArraySegment<byte>(
                            Encoding.UTF8.GetBytes(message));
                    Console.WriteLine("Sending message: " + message);
                    await this.webSocket.SendAsync(bytesToSend, WebSocketMessageType.Text, true, this.cancellationToken);
                    Console.WriteLine("Message sent: " + message);
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
            Task checkStateTask = null;
            var allTasks = new List<Task> { sendTask, receiveTask };
            if (this.enableCheckWebSocketStateDebugging)
            {
                checkStateTask = Task.Run(() => this.CheckWebSocketState());
                allTasks.Insert(0, checkStateTask);
            }

            var finished = await Task.WhenAny(allTasks);
            /*In case of resource issues, we could try to dispose the tasks, given they must be finished by now, 
            because they can only return when the cancellation token is cancelled.*/
            /*foreach (var task in allTasks)
                task.Dispose();*/
            if (allTasks.Contains(finished))
            {
                if (finished == sendTask)
                    Console.WriteLine("Finished handling task: sending.");
                else if (finished == receiveTask)
                    Console.WriteLine("Finished handling task: receiving.");
                else if (finished == checkStateTask)
                    Console.WriteLine("Finished handling task: checkState.");
            }
            else
                Console.WriteLine("Finished handling task: unknown.");
            Console.WriteLine("Handling finished.");
            Console.WriteLine("Cancellation requested: " + this.cancellationToken.IsCancellationRequested);
            Console.WriteLine("Thread state at handler end: " + Thread.CurrentThread.ThreadState);
        }

        private void CheckWebSocketState()
        {
            while(true)
            {
                this.cancellationToken.ThrowIfCancellationRequested();
                if(this.webSocket.State != WebSocketState.Open)
                {
                    Console.WriteLine("WebSocketState not open: " + this.webSocket.State + " at " + DateTime.Now.ToString());
                    Debug.WriteLine("WebSocketState not open: " + this.webSocket.State + " at " + DateTime.Now.ToString());
                }
            }
            
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
        /// retries once afterwards. 
        /// </summary>
        /// <list type="table">
        ///     <item>
        ///         <term>Post</term>
        ///         <description>Thrown <c>Exception</c>s are in <c>this.exceptionQueue</c>.</description>
        ///     </item>
        /// </list>
        private async Task CommunicateWithServerAsync()
        {
            try
            {
                while (true)
                {
                    Console.WriteLine("Communicate loop.");
                    this.cancellationToken.ThrowIfCancellationRequested();

                    // Work-around to ignore aborted websocket. This is not a decent fix, but the problem usually lies at the server side.
                    if (this.webSocket == null || this.webSocket.State == WebSocketState.Aborted)
                    {
                        if (this.webSocket != null)
                            // Connection was aborted for som unknown reason. Wait some time before trying to reconnect. Exponential averaging in real life.
                            await Task.Delay(100, this.cancellationToken);
                        this.webSocket = new ClientWebSocket();
                        //this.webSocket.Options.SetRequestHeader("Authorization", "Basic " + Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes("user:password")));
                        this.webSocket.Options.SetRequestHeader("Authorization", this.authorization);
                    }
                    if (this.webSocket.State != WebSocketState.Open)
                    {
                            
                        Console.WriteLine("Websocket NOT connected. Trying to connect. " + this.connectionTimeoutSeconds + "s timeout set.");
                        Debug.WriteLine("Websocket NOT connected. Trying to connect. " + this.connectionTimeoutSeconds + "s timeout set.");
                        await this.ConnectToServerAsync();
                        Console.WriteLine("Connection established.");
                        await this.webSocket.SendAsync(connectionEstablishedMessage, WebSocketMessageType.Text, true, this.cancellationToken);
                        Console.WriteLine("Confirmation message sent.");
                    }
                    await this.HandleSendReceiveTasksAsync();
                }
            } 
            catch(OperationCanceledException)
            {
                Console.WriteLine("Web socket connection closed by calling class instance.");
            }
            catch (Exception e)
            {
                // Add the exception to the queue
                //if (e is InvalidOperationException)
                    //e = new Exception("WebSocket State after " + i + " loops: " + oldState + e.Message);
                this.exceptionQueue.Enqueue(e);
            }
            finally
            {
                //if (this.websocket != null & this.websocket.State == WebSocketState.Open)
                // close websocket
                try
                {
                    if (this.webSocket != null && this.webSocket.State == WebSocketState.Open)
                        await this.webSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, "Unexpected exception thrown.", CancellationToken.None);
                } 
                catch(Exception)
                {
                    Debug.WriteLine("Connection could not be closed properly.");
                }
                Debug.WriteLine("Communication with server ended.");
                Console.WriteLine("Communication with server ended.");
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
