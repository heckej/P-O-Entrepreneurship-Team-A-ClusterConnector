using System;
using System.Collections.Generic;
using System.Text;

namespace ClusterClient.Models
{
    class UserAnswersMessage : UserMessage
    {
        private readonly List<UserAnswer> AnswerQuestions = new List<UserAnswer>();

        public void addAnswer(UserAnswer answer)
        {
            this.AnswerQuestions.Add(answer);
        }
    }
}
