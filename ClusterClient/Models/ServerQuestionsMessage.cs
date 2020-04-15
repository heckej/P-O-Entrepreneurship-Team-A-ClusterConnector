using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace ClusterClient.Models
{
    public class ServerQuestionsMessage : ServerMessage
    {
        public List<ServerQuestion> AnswerQuestions { get; set; }
    }
}
