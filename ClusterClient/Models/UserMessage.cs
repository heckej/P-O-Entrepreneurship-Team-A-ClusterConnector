using System;
using System.Collections.Generic;
using System.Text;

namespace ClusterClient.Models
{
    public abstract class UserMessage
    {
        public string user_id { get; set; }

        public abstract string ToJson();
    }
}
