using System.Collections.Generic;

namespace ClusterClient.Models
{
    public class Requests
    {
        public const string UnansweredQuestions = "answer_questions";
        public const string Default = "no_request";
        public static ISet<string> GetRequests()
        {
            return new HashSet<string>
            {
                UnansweredQuestions,
            };
        }
    }
}
