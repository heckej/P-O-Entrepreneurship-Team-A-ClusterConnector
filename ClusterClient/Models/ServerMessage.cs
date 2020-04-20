using System;
using System.Collections.Generic;
using System.Text;

namespace ClusterClient.Models
{
    public class ServerMessage
    {
        public string action { get; set; }
        public string user_id { get; set; }
        public string status_msg { get; set; }
        public int status_code { get; set; }
    }
}
