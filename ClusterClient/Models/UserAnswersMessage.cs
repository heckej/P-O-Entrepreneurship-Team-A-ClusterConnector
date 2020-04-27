using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Text.Json;

namespace ClusterClient.Models
{
    public class UserAnswersMessage : UserMessage
    {
        private List<UserAnswer> _answer_questions = new List<UserAnswer>();

        public List<UserAnswer> answer_questions {
            get  => this._answer_questions;
            set
            {
                foreach(UserAnswer answer in value)
                    this.AddAnswer(answer);
            }
        }

        public void AddAnswer(UserAnswer answer)
        {
            this._answer_questions.Add(answer);
        }

        public void AddAnswers(ICollection<UserAnswer> userAnswers)
        {
            this._answer_questions = this._answer_questions.Union(userAnswers).ToList();
            //((ISet<UserAnswer>) this._answer_questions).UnionWith(userAnswers);
        }

        public override string ToJson()
        {
            return JsonSerializer.Serialize<UserAnswersMessage>(this);
        }
    }
}
