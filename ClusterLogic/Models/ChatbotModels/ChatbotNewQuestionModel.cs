using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterLogic.Models.ChatbotModels
{
    public class ChatbotNewQuestionModel
    {
        private int _user_id = -1;
        private String _question = null;
        private int _chatbot_temp_id = -1;

        public int user_id { get => _user_id; set => _user_id = value; }
        public string question { get => _question; set => _question = value; }
        public int chatbot_temp_id { get => _chatbot_temp_id; set => _chatbot_temp_id = value; }
    }
}
