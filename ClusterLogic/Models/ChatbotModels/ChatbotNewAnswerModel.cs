using ClusterConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterLogic.Models.ChatbotModels
{
    //From wiki https://github.com/heckej/P-O-Entrepreneurship-Team-A-code/wiki/Server-to-Chatbot-Communication
    public class ChatbotNewAnswerModel
    {
        private string _action = "answers";
        private string _user_id = null;
        private String _question = null;
        private int _question_id = -1;
        private int _chatbot_temp_id = -1;
        private int _answer_id = -1;
        private String _answer = null;
        private float _certainty = -1;

        public string user_id { get => _user_id; set => _user_id = value; }
        public string question { get => _question; set => _question = value; }
        public int question_id { get => _question_id; set => _question_id = value; }
        public int chatbot_temp_id { get => _chatbot_temp_id; set => _chatbot_temp_id = value; }
        public int answer_id { get => _answer_id; set => _answer_id = value; }
        public string answer { get => _answer; set => _answer = value; }
        public float certainty { get => _certainty; set => _certainty = value; }
        public string action { get => _action; set => _action = value; }

        public ChatbotNewAnswerModel(string user_id, string question, int question_id, int temporary_chatbot_id, string answer, int answer_id, int certainty)
        {
            _user_id = user_id;
            _question = question;
            _question_id = question_id;
            _chatbot_temp_id = temporary_chatbot_id;
            _answer_id = answer_id;
            _answer = answer;
            _certainty = certainty;
        }

        public ChatbotNewAnswerModel(MatchQuestionLogicResponse result)
        {
            if (ServerUtilities.msgIdToUserID[result.Msg_id] is NewQuestion)
            {
                NewQuestion temp = (NewQuestion)ServerUtilities.msgIdToUserID[result.Msg_id];
                this.question_id = result.Question_id;
                this.certainty = 0.75f;
                this.question = temp.question;
                this.user_id = temp.user_id;
                this.answer = result.Answer;
                this.answer_id = LogicUtility.getAnswerId(result.Answer, this.question_id);
                this.chatbot_temp_id = temp.chatbot_temp_id;
            }
            else if (ServerUtilities.msgIdToUserID[result.Msg_id] is NewOpenQuestion)
            {
                NewOpenQuestion temp = (NewOpenQuestion)ServerUtilities.msgIdToUserID[result.Msg_id];
                this.question_id = result.Question_id;
                this.certainty = 0.75f;
                this.question = temp.question;
                this.user_id = temp.user_id;
                this.answer = result.Answer;
                this.answer_id = LogicUtility.getAnswerId(result.Answer, this.question_id);
                this.chatbot_temp_id = temp.chatbot_temp_id;
            }

        }
    }
}
