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

}

