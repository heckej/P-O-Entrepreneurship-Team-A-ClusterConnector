using System;
using System.Collections.Generic;
using System.Text;

namespace ClusterClient.Models
{
    public class ServerMessage
    {
        public string Action { get; set; }
        public int UserID { get; set; }
        public string StatusMessage { get; set; }
        public int StatusCode { get; set; }
    }
}
