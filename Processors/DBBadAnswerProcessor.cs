using ClusterConnector.Models.Database;
using ClusterConnector.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterConnector.Processors
{
    public class DBBadAnswerProcessor
    {
        public DBBadAnswer getByKey(int answer_id)
        {
            String sqlCommand = "Select * From dbo.BadAnswers answer Where answer.bad_answer_id = " + answer_id + ";";
            DBManager manager = new DBManager(true);
            var reader = manager.Read(sqlCommand);
            if (!reader.Read())
            {
                return null;
            }

            DBBadAnswer answer = new DBBadAnswer();
            answer.Bad_answer_id = (int)reader["bad_answer_id"];
            answer.Bad_answer = (String)reader["bad_answer"];
            answer.Question_id = (int)reader["question_id"];
            answer.Author_user_id = (int)reader["author_user_id"];

            manager.Close();

            return answer;
        }

        public List<DBBadAnswer> getByKeys(List<int> keys)
        {
            List<DBBadAnswer> result = new List<DBBadAnswer>();
            DBManager manager = new DBManager(true);

            String sqlCommand = "Select * From dbo.BadAnswers answer Where ";

            for (int i = 0; i < keys.Count; i++)
            {
                if (i != keys.Count-1)
                {
                    sqlCommand += "answer.bad_answer_id = " + keys[i] + " OR ";
                }
                else
                {
                    sqlCommand += "answer.bad_answer_id = " + keys[i] + ";";
                }
            }
            var reader = manager.Read(sqlCommand);
            while (reader.Read())
            {
                DBBadAnswer answer = new DBBadAnswer();
                answer.Bad_answer_id = (int)reader["bad_answer_id"];
                answer.Bad_answer = (String)reader["bad_answer"];
                answer.Question_id = (int)reader["question_id"];
                answer.Author_user_id = (int)reader["author_user_id"];

                result.Add(answer);
            }



            manager.Close();
            return result;
        }
    }
}
