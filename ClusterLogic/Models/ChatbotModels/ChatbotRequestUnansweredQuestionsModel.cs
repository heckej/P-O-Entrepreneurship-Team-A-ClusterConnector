using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterLogic.Models.ChatbotModels
{
    /// <summary>
    /// based on:
    ///     We vragen x onbeantwoorde vragen op uit de database. 
    ///     We maken koppeling met een endpoint van jullie API en verwachten een JSON terug 
    ///     met 3 verschillende elementen met als parameters {Question, QuestionID}
    /// </summary>
    /// 
    /// What does a proper request look like?
    /// From Chatbot -> ClusterAPI on api/Chatbot/WS json: {"request_marker":"true"}
    public class ChatbotRequestUnansweredQuestionsModel : BaseModel
    {
        private bool _request_marker = false;

        public bool request_marker { get => _request_marker; set => _request_marker = value; }

        public bool IsComplete()
        {
            return _request_marker != false;
        }
    }

    public class ChatbotRequestUnansweredQuestionsResponseModel : BaseModel
    {
        private int _user_id = -1;
        private int _question_id = -1;

        public int question_id { get => _question_id; set => _question_id = value; }
        public int user_id { get => _user_id; set => _user_id = value; }

        public bool IsComplete()
        {
            return _user_id != -1 && _question_id != -1;
        }
    }
}
