using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterConnector.Models.NLP
{
    public class NLPAction
    {
        private String action;
        private int question_id;
        private String question;
        private List<NLPQuestion> compare_questions;
        private int msg_id;

        public string Action { get => action; set => action = value; }
        public int Question_id { get => question_id; set => question_id = value; }
        public string Question { get => question; set => question = value; }
        public int Msg_id { get => msg_id; set => msg_id = value; }
        public List<NLPQuestion> Compare_questions { get => compare_questions; set => compare_questions = value; }
    }
}
