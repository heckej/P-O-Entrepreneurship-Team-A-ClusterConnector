﻿using System;
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
        private String _user_id;
        private String _request;

        public string user_id { get => _user_id; set => _user_id = value; }
        public string request { get => _request; set => _request = value; }

        public bool IsComplete()
        {
            return user_id != null && request != null;
        }
    }

    public class ChatbotRequestUnansweredQuestionsResponseModel : BaseModel
    {
        private string _user_id = null;
        private int _question_id = -1;

        public int question_id { get => _question_id; set => _question_id = value; }
        public string user_id { get => _user_id; set => _user_id = value; }

        public bool IsComplete()
        {
            return _user_id != null && _question_id != -1;
        }
    }
}
