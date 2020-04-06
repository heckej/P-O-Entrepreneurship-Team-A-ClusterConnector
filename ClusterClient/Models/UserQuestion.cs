using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Diagnostics;

namespace ClusterClient.Models
{
    class UserQuestion : UserMessage
    {
        public string Question { get; set; }
        public int QuestionID { get; set; }
    }
}
