using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ClusterClient
{
    class WebsocketThread
    {
        public WebsocketThread(string websocketURI, Queue<Exception> exceptionQueue, MethodToPassMessageToClient passMessageToClient,
            Queue<string> replyQueue, int connectionTimeout)
        {
            this.websocketURI = websocketURI;
            this.exceptionQueue = exceptionQueue;
            this.methodToPassMessageToClient = passMessageToClient;
            this.replyQueue = replyQueue;
            this.connectionTimeout = connectionTimeout;
        }

        private string websocketURI;

        private Queue<Exception> exceptionQueue;

        private Queue<string> replyQueue;

        private int connectionTimeout;

        public delegate void MethodToPassMessageToClient(string messageFromServer);

        private MethodToPassMessageToClient methodToPassMessageToClient = null;

        private async Task GetRepliesToSend()
        {
            
        }

        private async Task ReceiveHandler()
        {

        }

        private async Task ProcessReceivedMessage(string message)
        {

        }

        private async Task SendHandler()
        {

        }

        private async Task Handler()
        {

        }

        private async Task CommunicateWithServer()
        {

        }

        private async Task ConnectToServer()
        {

        }


    }
}
