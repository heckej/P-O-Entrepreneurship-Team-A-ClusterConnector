using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterConnector.Models.Database
{
    public class DBBadAnswer : DBData
    {
        private int bad_answer_id;
        private String bad_answer;
        private int question_id;
        private int author_user_id;

        public int Bad_answer_id { get => bad_answer_id; set => bad_answer_id = value; }
        public string Bad_answer { get => bad_answer; set => bad_answer = value; }
        public int Question_id { get => question_id; set => question_id = value; }
        public int Author_user_id { get => author_user_id; set => author_user_id = value; }
    }
}
