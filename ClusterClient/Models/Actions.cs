using System;
using System.Collections.Generic;
using System.Text;

namespace ClusterClient.Models
{
    public class Actions
    {
        public const string Questions = "questions";
        public const string Answer = "answers";
        public const string Default = "no_action";
        public static ISet<string> GetActions()
        {
            return new HashSet<string>
            {
                Questions,
                Answer
            };
        }
    }
}
