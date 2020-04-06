using System;
using System.Collections.Generic;
using System.Text;

namespace ClusterClient.Models
{
    public class ServerAnswer : ServerMessage
    {
        public int UserID { get; set; }
        public int QuestionID { get; set; }
        public string Question { get; set; }
        public int AnswerID { get; set; }
        public string Answer { get; set; }
        public int ChatbotTempID { get; set; }
        public double Certainty { get; set; }
    }
}
