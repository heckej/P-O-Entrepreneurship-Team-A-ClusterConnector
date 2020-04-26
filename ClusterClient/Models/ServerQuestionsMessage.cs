using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace ClusterClient.Models
{
    public class ServerQuestionsMessage : ServerMessage
    {
        private List<ServerQuestion> _answer_questions = new List<ServerQuestion>();
        public List<ServerQuestion> answer_questions { get => _answer_questions; set => _answer_questions = value; }
    }
}
