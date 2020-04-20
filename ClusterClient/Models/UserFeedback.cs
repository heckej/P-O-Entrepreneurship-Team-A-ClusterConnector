using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace ClusterClient.Models
{
    class UserFeedback : UserMessage
    {
        public int question_id { get; set; }
        public int answer_id { get; set; }
        public int feedback_code { get; set; }
        public override string ToJson()
        {
            return JsonSerializer.Serialize<UserFeedback>(this);
        }
    }
}
