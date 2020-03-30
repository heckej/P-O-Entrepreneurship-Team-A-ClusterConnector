using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterConnector.Models.Database
{
    public class DBAnswer : DBData
    {
        private int answer_id;
        private String answer;
        private int user_id;
        private String feedback_history;
        private String last_moderated;

        public int Answer_id { get => answer_id; set => answer_id = value; }
        public string Answer { get => answer; set => answer = value; }
        public int User_id { get => user_id; set => user_id = value; }
        public string Feedback_history { get => feedback_history; set => feedback_history = value; }
        public String Last_moderated { get => last_moderated; set => last_moderated = value; }
    }
}
