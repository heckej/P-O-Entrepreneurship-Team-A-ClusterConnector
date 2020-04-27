using System;
using System.Collections.Generic;
using System.Text;

namespace ClusterClient.Models
{
    
    public class ServerAnswer : ServerMessage
    {
        private int _question_id = -1;
        private string _question = null;
        private int _answer_id = -1;
        private string _answer = null;
        private int _chatbot_temp_id = -1;
        private double _certainty = 0;

        public int question_id { get => _question_id; set => _question_id = value; }
        public string question { get => _question; set => _question = value; }
        public int answer_id { get => _answer_id; set => _answer_id = value; }
        public string answer { get => _answer; set => _answer = value; }
        public int chatbot_temp_id { get => _chatbot_temp_id; set => _chatbot_temp_id = value; }
        public double certainty { get => _certainty; set => _certainty = value; }
    }
}
