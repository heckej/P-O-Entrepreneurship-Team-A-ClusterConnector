using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Web;

namespace ClusterAPI.Utilities.Sockets
{
    public class ClientManager
    {
        public void run()
        {
            var url = "https://clusterapidebug.azurewebsites.net/";
            Uri myUri = new Uri(url);
            var ip = Dns.GetHostAddresses(myUri.Host)[0];
            ip = IPAddress.Parse("127.0.0.1");

            TcpListener tcpListener = new TcpListener(ip, 80);
            tcpListener.Start();

            while (true)
            {
                TcpClient client = tcpListener.AcceptTcpClient();
                ServerClient serverClient = new ServerClient(client,tcpListener);
                Thread clientThread = new Thread(new ThreadStart(serverClient.run));
                clientThread.Start();
            }
        }
    }

    public class ServerClient
    {
        private readonly TcpClient tcpClient;
        private readonly TcpListener server;
        public ServerClient(TcpClient tcpClient, TcpListener server)
        {
            this.tcpClient = tcpClient;
            this.server = server;
        }

        public void run()
        {
            System.Console.WriteLine("Opened Client");

            string data = null;
            byte[] bytes = null;

            while (true)
            {
                bytes = new byte[1024];
                int bytesRec = tcpClient.Client.Receive(bytes);
                data += Encoding.ASCII.GetString(bytes, 0, bytesRec);

                System.Diagnostics.Debug.WriteLine(data);
                //server.Server.Send(Encoding.ASCII.GetBytes(data));

                if (data.IndexOf("<EOF>") > -1)
                {
                    break;
                }
            }

        }
    }
}