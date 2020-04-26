using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace ClusterClient.Models
{
    class UserFeedback : UserMessage
    {
        private int _question_id = -1;
        private int _answer_id = -1;
        private int _feedback_code = -1;

        public int question_id { get => _question_id; set => _question_id = value; }
        public int answer_id { get => _answer_id; set => _answer_id = value; }
        public int feedback_code { get => _feedback_code; set => _feedback_code = value; }
        public override string ToJson()
        {
            return JsonSerializer.Serialize<UserFeedback>(this);
        }
    }
}
