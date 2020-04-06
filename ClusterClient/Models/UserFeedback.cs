using System;
using System.Collections.Generic;
using System.Text;

namespace ClusterClient.Models
{
    class UserFeedback : UserMessage
    {
        public int QuestionID { get; set; }
        public int AnswerID { get; set; }
        public int FeedbackCode { get; set; }
    }
}
