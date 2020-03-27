using ClusterConnector.Models.Database;
using ClusterConnector.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterConnector.Processors
{
    public class DBOpenAnswerProcessor
    {
        public DBOpenAnswer getByKey(int answer_id)
        {
            String sqlCommand = "Select * From dbo.OpenAnswers answer Where answer.question_id = " + answer_id + ";";
            DBManager manager = new DBManager(true);
            var reader = manager.Read(sqlCommand);
            if (!reader.Read())
            {
                return null;
            }

            DBOpenAnswer answer = new DBOpenAnswer();
            answer.Question_id = (int)reader["question_id"];
            answer.User_id = (int)reader["user_id"];
            answer.Probability = (float)reader["probability"];

            manager.Close();

            return answer;
        }

        public List<DBOpenAnswer> getByKeys(List<int> keys)
        {
            List<DBOpenAnswer> result = new List<DBOpenAnswer>();
            DBManager manager = new DBManager(true);

            String sqlCommand = "Select * From dbo.OpenAnswers answer Where ";

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
                DBOpenAnswer answer = new DBOpenAnswer();
                answer.Question_id = (int)reader["question_id"];
                answer.User_id = (int)reader["user_id"];
                answer.Probability = (float)reader["probability"];

                result.Add(answer);
            }



            manager.Close();
            return result;
        }
    }
}
