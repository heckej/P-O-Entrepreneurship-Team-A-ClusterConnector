using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;


namespace ClusterClient.Models
{
    public class UserAnswersMessage : UserMessage
    {
        private List<UserAnswer> answerQuestions = new List<UserAnswer>();

        public List<UserAnswer> answer_questions {
            get  => this.answerQuestions;
            set
            {
                foreach(UserAnswer answer in value)
                    this.AddAnswer(answer);
            }
        }

        public void AddAnswer(UserAnswer answer)
        {
            this.answerQuestions.Add(answer);
        }

        public void AddAnswers(ICollection<UserAnswer> userAnswers)
        {
            this.answerQuestions = this.answerQuestions.Union(userAnswers).ToList();
            //((ISet<UserAnswer>) this.answerQuestions).UnionWith(userAnswers);
        }
    }
}
