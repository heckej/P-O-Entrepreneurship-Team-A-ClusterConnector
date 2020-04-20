using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace ClusterClient.Models
{
    class UserRequest : UserMessage
    {
        public string request { get; set; }

        public override string ToJson()
        {
            return JsonSerializer.Serialize<UserRequest>(this);
        }
    }
}
