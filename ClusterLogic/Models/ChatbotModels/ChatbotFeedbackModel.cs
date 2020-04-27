using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterLogic.Models.ChatbotModels
{
    public class ChatbotFeedbackModel : BaseModel
    {
        private string _user_id;
        private string _action;
        private int _question_id;
        private int _answer_id;
        private int _feedback_code;

        public ChatbotFeedbackModel() { }

        public ChatbotFeedbackModel(Dictionary<string, string> dict)
        {
            dict.TryGetValue("user_id", out _user_id);
            dict.TryGetValue("action", out _action);
            String temp = null;
            dict.TryGetValue("question_id", out temp);
            if (temp != null)
            {
                int.TryParse(temp,out _question_id);
            }
            dict.TryGetValue("answer_id", out temp);
            if (temp != null)
            {
                int.TryParse(temp, out _answer_id);
            }
            dict.TryGetValue("feedback_code", out temp);
            if (temp != null)
            {
                int.TryParse(temp, out _feedback_code);
            }
        }

        public string action { get => _action; set => _action = value; }
        public int question_id { get => _question_id; set => _question_id = value; }
        public int answer_id { get => _answer_id; set => _answer_id = value; }
        public int feedback_code { get => _feedback_code; set => _feedback_code = value; }
        public string user_id { get => _user_id; set => _user_id = value; }

        public bool IsComplete()
        {
            return true;
        }
    }
}
