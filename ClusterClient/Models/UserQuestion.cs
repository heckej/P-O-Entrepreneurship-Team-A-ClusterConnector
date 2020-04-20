using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Diagnostics;

namespace ClusterClient.Models
{
    class UserQuestion : UserMessage
    {
        public string question { get; set; }
        public int chatbot_temp_id { get; set; }
    }
}
