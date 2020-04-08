using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterConnector.Models.Database
{
    public class DBOpenAnswer : DBData
    {
        private int question_id;
        private int user_id;
        private float probability;

        public int Question_id { get => question_id; set => question_id = value; }
        public int User_id { get => user_id; set => user_id = value; }
        public float Probability { get => probability; set => probability = value; }
    }
}
