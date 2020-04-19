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

		private int _user_id = -1; 
		private ChatbotQuestionHasNoAnswerModel[] _openQuestions = null; 

		public int user_id { get => _user_id; set => _user_id = value; }
		public ChatbotQuestionHasNoAnswerModel[] openQuestions { get => _openQuestions; set => _openQuestions = value; }

		public ChatbotResponseUnansweredQuestionsModel(List<ChatbotQuestionHasNoAnswerModel> openQuestions, int userID)
        {
			_user_id = userId;
			_openQuestions = openQuestions; 
        }

	}

}

