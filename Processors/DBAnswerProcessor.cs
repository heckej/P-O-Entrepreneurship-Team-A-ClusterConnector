using ClusterConnector.Models.Database;
using ClusterConnector.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterConnector.Processors
{
    public class DBAnswerProcessor
    {
        public DBAnswer getByKey(int answer_id)
        {
            String sqlCommand = "Select * From dbo.Answers answer Where answer.answer_id = " + answer_id + ";";
            DBManager manager = new DBManager(true);
            var reader = manager.Read(sqlCommand);
            if (!reader.Read())
            {
                return null;
            }

            DBAnswer answer = new DBAnswer();
            answer.Answer_id = (int)reader["answer_id"];
            answer.Answer = (String)reader["answer"];
            answer.Feedback_history = (String)reader["feedback_history"];
            answer.User_id = (int)reader["user_id"];
            answer.Last_moderated = ((DateTime)reader["last_moderated"]).ToString(ServerUtilities.DATE_TIME_FORMAT);


            manager.Close();

            return answer;
        }

        public List<DBAnswer> getByKeys(List<int> keys)
        {
            List<DBAnswer> result = new List<DBAnswer>();
            DBManager manager = new DBManager(true);

            String sqlCommand = "Select * From dbo.Answers answer Where ";

            for (int i = 0; i < keys.Count; i++)
            {
                if (i != keys.Count-1)
                {
                    sqlCommand += "answer.answer_id = " + keys[i] + " OR ";
                }
                else
                {
                    sqlCommand += "answer.answer_id = " + keys[i] + ";";
                }
            }
            var reader = manager.Read(sqlCommand);
            while (reader.Read())
            {
                DBAnswer answer = new DBAnswer();
                answer.Answer_id = (int)reader["answer_id"];
                answer.Answer = (String)reader["answer"];
                answer.Feedback_history = (String)reader["feedback_history"];
                answer.User_id = (int)reader["user_id"];
                answer.Last_moderated = ((DateTime)reader["last_moderated"]).ToString(ServerUtilities.DATE_TIME_FORMAT);

                result.Add(answer);
            }



            manager.Close();
            return result;
        }
    }
}
