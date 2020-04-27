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

        public ServerResponseNoAnswerToQuestion(MatchQuestionLogicResponse result, MatchQuestionModelResponse baseModel, int v)
        {
            {
                NewOpenQuestion temp = (NewOpenQuestion)ServerUtilities.msgIdToUserID[baseModel.msg_id];
                user_id = temp.user_id;
                question = temp.question;
                chatbot_temp_id = temp.chatbot_temp_id;
                question_id =v;
            }
        }

        public string action { get => _action; set => _action = value; }
        public string user_id { get => _user_id; set => _user_id = value; }
        public int question_id { get => _question_id; set => _question_id = value; }
        public string question { get => _question; set => _question = value; }
        public int chatbot_temp_id { get => _chatbot_temp_id; set => _chatbot_temp_id = value; }
    }

    public class ServerAnswerAfterQuestion
    {
        private string _action = "answers";
        private string _user_id = null;
        private int _question_id;
        private int _answer_id;
        private string _answer;
        private float _certainty;
        private string _question = null;

        public ServerAnswerAfterQuestion(object openAnswerModel, NewAnswerOffenseCheck newAnswerOffenseCheck, OffensivenessLogicResponse result, int answerId, string question)
        {
            user_id = newAnswerOffenseCheck.user_id; //openAnswerModel.user_id
            question_id = result.Question_id;
            answer_id = answerId;
            answer = newAnswerOffenseCheck.answer;
            certainty = .75f;
            this.question = question;
        }

        public string user_id { get => _user_id; set => _user_id = value; }
        public int question_id { get => _question_id; set => _question_id = value; }
        public int answer_id { get => _answer_id; set => _answer_id = value; }
        public string action { get => _action; set => _action = value; }
        public string answer { get => _answer; set => _answer = value; }
        public float certainty { get => _certainty; set => _certainty = value; }
        public string question { get => _question; set => _question = value; }
    }
}
