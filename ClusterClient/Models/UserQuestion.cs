using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Diagnostics;

namespace ClusterClient.Models
{
    class UserQuestion : UserMessage
    {
        private string _question = null;
        private int _chatbot_temp_id = -1;

        public string question { get => _question; set => _question = value; }
        public int chatbot_temp_id { get => _chatbot_temp_id; set => _chatbot_temp_id = value; }

        public override string ToJson()
        {
            return JsonSerializer.Serialize<UserQuestion>(this);
        }
    }
}
