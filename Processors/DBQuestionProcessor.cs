using ClusterConnector.Models.Database;
using ClusterConnector.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterConnector.Processors
{
    public class DBQuestionProcessor
    {
        public DBQuestion getByKey(int answer_id)
        {
            String sqlCommand = "Select * From dbo.Questions answer Where answer.question_id = " + answer_id + ";";
            DBManager manager = new DBManager(true);
            var reader = manager.Read(sqlCommand);
            if (!reader.Read())
            {
                return null;
            }

            DBQuestion answer = new DBQuestion();
            answer.Question_id = (int)reader["question_id"];
            answer.Question = (String)reader["question"];
            answer.Answer_id = (int)reader["answer_id"];

            manager.Close();

            return answer;
        }

        public List<DBQuestion> getByKeys(List<int> keys)
        {
            List<DBQuestion> result = new List<DBQuestion>();
            DBManager manager = new DBManager(true);

            String sqlCommand = "Select * From dbo.Questions answer Where ";

            for (int i = 0; i < keys.Count; i++)
            {
                if (i != keys.Count-1)
                {
                    sqlCommand += "answer.question_id = " + keys[i] + " OR ";
                }
                else
                {
                    sqlCommand += "answer.question_id = " + keys[i] + ";";
                }
            }
            var reader = manager.Read(sqlCommand);
            while (reader.Read())
            {
                DBQuestion answer = new DBQuestion();
                answer.Question_id = (int)reader["question_id"];
                answer.Question = (String)reader["question"];
                answer.Answer_id = (int)reader["answer_id"];

                result.Add(answer);
            }



            manager.Close();
            return result;
        }
    }
}
