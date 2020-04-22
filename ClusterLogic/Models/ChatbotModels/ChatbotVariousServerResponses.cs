using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClusterConnector;
using ClusterLogic.ChatbotHandler;

namespace ClusterLogic.Models.ChatbotModels
{
    public class ChatbotVariousServerResponses
    {
        private String _action = "answers";
        private String _user_id = null;
        private int _question_id = -1;
        private String _status_msg = null;
        private int _status_code = -1;

        public ChatbotVariousServerResponses(NewQuestionNonsenseCheck newQuestionNonsenseCheck, bool isNonsense = true)
        {
            if (isNonsense)
            {
                question_id = newQuestionNonsenseCheck.question_id;
                status_msg = "The given question was nonsense.";
                status_code = 2;
                user_id = newQuestionNonsenseCheck.user_id;
            }
            else
            {
                question_id = newQuestionNonsenseCheck.question_id;
                status_msg = "The given question was offensive.";
                status_code = 3;
                user_id = newQuestionNonsenseCheck.user_id;
            }

        }

        public string action { get => _action; set => _action = value; }
        public string user_id { get => _user_id; set => _user_id = value; }
        public int question_id { get => _question_id; set => _question_id = value; }
        public string status_msg { get => _status_msg; set => _status_msg = value; }
        public int status_code { get => _status_code; set => _status_code = value; }
    }

    public class ServerResponseNoAnswerToQuestion
    {
        private string _action = "answers";
        private String _user_id = null;
        private int _question_id = -1;
        private String _question = null;
        private int _chatbot_temp_id = -1;

        public ServerResponseNoAnswerToQuestion(MatchQuestionLogicResponse new_result, MatchQuestionModelResponse baseModel)
        {
            {
                NewOpenQuestion temp = (NewOpenQuestion)ServerUtilities.msgIdToUserID[baseModel.msg_id];
                user_id = temp.user_id;
                question = temp.question;
                chatbot_temp_id = temp.chatbot_temp_id;
                question_id = ProcessChatbotLogic.assignQuestionIdToNewQuestion(temp);
            }
        }

        public string action { get => _action; set => _action = value; }
        public string user_id { get => _user_id; set => _user_id = value; }
        public int question_id { get => _question_id; set => _question_id = value; }
        public string question { get => _question; set => _question = value; }
        public int chatbot_temp_id { get => _chatbot_temp_id; set => _chatbot_temp_id = value; }
    }
}
