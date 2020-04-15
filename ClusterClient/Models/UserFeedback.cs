using System;
using System.Collections.Generic;
using System.Text;

namespace ClusterClient.Models
{
    class UserFeedback : UserMessage
    {
        public int question_id { get; set; }
        public int answer_id { get; set; }
        public int feedback_code { get; set; }
    }
}
