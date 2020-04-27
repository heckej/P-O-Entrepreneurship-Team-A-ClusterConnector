using System;
using System.Collections.Generic;
using System.Text;

namespace ClusterClient.Models
{
    public class ServerMessage
    {
        private string _action = Actions.Default;
        private string _user_id = null;
        private string _status_msg = null;
        private int _status_code = 0;

        public string action { get => _action; set => _action = value; }
        public string user_id { get => _user_id; set => _user_id = value; }
        public string status_msg { get=> _status_msg; set => _status_msg = value; }
        public int status_code { get => _status_code; set => _status_code = value; }
    }
}
