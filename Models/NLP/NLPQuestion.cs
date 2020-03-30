using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterConnector.Models.NLP
{
    public class NLPQuestion
    {
        private int question_id;

        public int Question_id { get => question_id; set => question_id = value; }

        private String question;

        public string Question { get => question; set => question = value; }
    }
}
