using System;
using System.Collections.Generic;
using System.Text;

namespace ClusterClient.Models
{
    class UserRequest : UserMessage
    {
        public string Request { get; set; }
    }
}
