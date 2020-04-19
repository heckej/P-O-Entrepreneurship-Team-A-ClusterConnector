using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterLogic.Models.ChatbotModels
{
    public class ChatbotQuestionHasNoAnswerModel
    {
        private int _user_id = -1;
        private String _question = null;
        private int _question_id = -1;
        private int _chatbot_temp_id = -1;

        public int user_id { get => _user_id; set => _user_id = value; }
        public string question { get => _question; set => _question = value; }
        public int question_id { get => _question_id; set => _question_id = value; }
        public int chatbot_temp_id { get => _chatbot_temp_id; set => _chatbot_temp_id = value; }

        public ChatbotQuestionHasNoAnswerModel(string question, int question_id, int userID = -1,  int chatbot_temp_id = -1) {
            _user_id = userID; 
            String _question = question; 
            _question_id = question_id; 
            _chatbot_temp_id = chatbot_temp_id; 
        }
        
    }
}
