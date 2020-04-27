using System;
using System.Collections.Generic;
using System.Text;

namespace ClusterClient.Models
{
    public class UserAnswer
    {
        private int _question_id = -1;
        private string _answer = null;

        public int question_id { get => _question_id; set => _question_id = value; }
        public string answer { get => _answer; set => _answer = value; }
    }
}
