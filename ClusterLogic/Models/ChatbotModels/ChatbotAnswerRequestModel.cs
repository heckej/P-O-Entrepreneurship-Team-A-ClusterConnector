using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterLogic.Models.ChatbotModels
{
    /// <summary>
    /// based on:
    ///     We sturen jullie een vraag, en willen - als mogelijk - 
    ///     een antwoord terugkrijgen. We maken koppeling met een endpoint van jullie API en versturen
    ///     een JSON met 1 element met als parameters {question, userID}. We verwachten als antwoord 
    ///     een JSON met 1 element en als parameters {userID, hasAnswer (boolean), answer, question}. 
    ///     Uiteraard kunnen mogen die twee laatste null zijn als hasAnswer=false
    ///
    /// </summary>
    public class ChatbotAnswerRequestModel : BaseModel
    {
        private int _question_id = -1;
        private string _user_id = null;

        public int question_id { get => _question_id; set => _question_id = value; }
        public string user_id { get => _user_id; set => _user_id = value; }

        public bool IsComplete()
        {
            return question_id != -1 && user_id != null;
        }
    }

    public class ChatbotAnswerRequestResponseModel : BaseModel
    {
        private string _user_id = null;
        private bool _has_answer = false;
        private int _answer_id = -1;
        private int _answer_question_id = -1;

        public string user_id { get => _user_id; set => _user_id = value; }
        public int answer_id { get => _answer_id; set => _answer_id = value; }
        public int answer_question_id { get => _answer_question_id; set => _answer_question_id = value; }
        public bool has_answer { get => _has_answer; set => _has_answer = value; }

        public ChatbotAnswerRequestResponseModel(string user_id, int answer_id, int answer_question_id, bool has_answer)
        {
            _user_id = user_id;
            _has_answer = has_answer;
            _answer_id = answer_id;
            _answer_question_id = answer_question_id;
        }

        public bool IsComplete()
        {
            return user_id != null;
        }
    }
}
