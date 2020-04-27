using System;
using System.Collections.Generic;
using System.Text;

namespace ClusterClient.Models
{
    public abstract class UserMessage
    {
        private string _user_id = null;

        public string user_id { get => _user_id; set => _user_id = value; }

        public abstract string ToJson();
    }
}
