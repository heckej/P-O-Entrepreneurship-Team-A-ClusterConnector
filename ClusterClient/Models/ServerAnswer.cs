using System;
using System.Collections.Generic;
using System.Text;

namespace ClusterClient.Models
{
    public class ServerAnswer : ServerMessage
    {
        public int question_id { get; set; }
        public string question { get; set; }
        public int answer_id { get; set; }
        public string answer { get; set; }
        public int chatbot_temp_id { get; set; }
        public double certainty { get; set; }
    }
}
