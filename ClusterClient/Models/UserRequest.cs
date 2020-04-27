using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace ClusterClient.Models
{
    class UserRequest : UserMessage
    {
        private string _request = Requests.Default;

        public string request { get => _request; set => _request = value; }

        public override string ToJson()
        {
            return JsonSerializer.Serialize<UserRequest>(this);
        }
    }
}
