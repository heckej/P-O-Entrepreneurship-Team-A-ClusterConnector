using System;
using System.Collections.Generic;
using System.Text;

namespace ClusterClient.Models
{
    public class ServerQuestion
    {
        private int _question_id = -1;
        private string _question = null;

        public int question_id { get => _question_id; set => _question_id = value; }
        public string question { get => _question; set => _question = value; }
    }
}
