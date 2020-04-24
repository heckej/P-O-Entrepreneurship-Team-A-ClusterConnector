using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterLogic.Models.ChatbotModels
{ 

	[Serializable]
	public class ChatbotResponseUnansweredQuestionsModel
	{

		private string _user_id = null; 
		private ChatbotQuestionHasNoAnswerModel[] _openQuestions = null; 

		public string user_id { get => _user_id; set => _user_id = value; }
		public ChatbotQuestionHasNoAnswerModel[] openQuestions { get => _openQuestions; set => _openQuestions = value; }

		public ChatbotResponseUnansweredQuestionsModel(ChatbotQuestionHasNoAnswerModel[] openQuestions, string userID)
        {
			_user_id = userID;
			_openQuestions = openQuestions; 
        }

	}

    public class ChatbotResponseUnansweredQuestionsModelToChatbot
    {
        private readonly string _action = "questions";
        private string _user_id = null;
        private ChatbotQuestionHasNoAnswerModelToChatbot[] _answer_questions = null;

        public string action { get => _action; }
        public string user_id { get => _user_id; set => _user_id = value; }
        public ChatbotQuestionHasNoAnswerModelToChatbot[] answer_questions { get => _answer_questions; set => _answer_questions = value; }

        public ChatbotResponseUnansweredQuestionsModelToChatbot(ChatbotResponseUnansweredQuestionsModel chatbotResponseUnansweredQuestionsModel)
        {
            _user_id = chatbotResponseUnansweredQuestionsModel.user_id;
            answer_questions = new ChatbotQuestionHasNoAnswerModelToChatbot[chatbotResponseUnansweredQuestionsModel.openQuestions.Length];
            for (int i = 0; i < chatbotResponseUnansweredQuestionsModel.openQuestions.Length; i++)
            {
                answer_questions[i] = new ChatbotQuestionHasNoAnswerModelToChatbot(chatbotResponseUnansweredQuestionsModel.openQuestions[i]);
            }
        }

    }

    public class ChatbotGivesAnswersToQuestionsToServer : BaseModel
    {

        private string _user_id = null;
        private ChatbotGivesAnswerModelToServer[] _answer_questions = null;

        public string user_id { get => _user_id; set => _user_id = value; }
        public ChatbotGivesAnswerModelToServer[] answer_questions { get => _answer_questions; set => _answer_questions = value; }

        public bool IsComplete()
        {
            return user_id != null && answer_questions != null;
        }
    }

}

