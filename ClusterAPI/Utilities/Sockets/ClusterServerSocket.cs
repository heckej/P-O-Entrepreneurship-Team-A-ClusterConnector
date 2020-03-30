using System;
using System.Net;
using System.Net.Sockets;


namespace ClusterAPI.Utilities.Sockets
{
    public class ClusterServerSocket
    {
        public ClusterServerSocket()
        {
            var url = "https://clusterapidebug.azurewebsites.net/";
            Uri myUri = new Uri(url);
            var ip = Dns.GetHostAddresses(myUri.Host)[0];

            TcpListener server = new TcpListener(ip,80);
        }
    }
}