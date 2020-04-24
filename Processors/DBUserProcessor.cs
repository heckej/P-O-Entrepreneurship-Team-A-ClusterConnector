using ClusterConnector.Models.Database;
using ClusterConnector.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Common;
using System.Data.SqlClient;

namespace ClusterConnector.Processors
{
    public class DBUserProcessor
    {
        public DBUser getByKey(string user_id)
        {
            String sqlCommand = "Select * From dbo.Users Where user_id = '" + user_id + "';";
            DBManager manager = new DBManager(true);
            var reader = manager.Read(sqlCommand);
            if (reader == null || !reader.Read())
            {
                return null;
            }

            DBUser answer = new DBUser();
            answer.User_id = (String)reader["user_id"];
            if (DBNull.Value != reader["last_active"])
            {
                answer.Last_active = ((DateTime)reader["last_active"]).ToString(ServerUtilities.DATE_TIME_FORMAT);
            }
            else
            {
                answer.Last_active = "NULL";
            }
            /*answer.Fname = (String)reader["fname"];
            answer.Lname = (String)reader["lname"];
            answer.Email = (String)reader["email"];
            answer.Phone = (String)reader["phone"];*/

            manager.Close();

            return answer;
        }

        public void AddUser(string user_id)
        {
            String sqlCommand = "INSERT INTO dbo.Users (user_id) VALUES('" + user_id + "');";
            DBManager manager = new DBManager(true);
            var reader = manager.Read(sqlCommand);
            if (reader == null || !reader.Read())
            {
                throw new Exception("User with user id " + user_id + " could not be added to the database.");
            }

            manager.Close();
        }

        public List<DBUser> getByKeys(List<int> keys)
        {
            List<DBUser> result = new List<DBUser>();
            DBManager manager = new DBManager(true);

            String sqlCommand = "Select * From dbo.Users _user Where ";

            for (int i = 0; i < keys.Count; i++)
            {
                if (i != keys.Count-1)
                {
                    sqlCommand += "_user.user_id = " + keys[i] + " OR ";
                }
                else
                {
                    sqlCommand += "_user.user_id = " + keys[i] + ";";
                }
            }
            var reader = manager.Read(sqlCommand);
            while (reader.Read())
            {
                DBUser answer = new DBUser();
                answer.User_id = (String)reader["user_id"];
                Object temp = reader["last_active"];
                if (DBNull.Value != temp)
                {
                    answer.Last_active = ((DateTime)reader["last_active"]).ToString(ServerUtilities.DATE_TIME_FORMAT);
                }
                else
                {
                    answer.Last_active = "NULL";
                }
                answer.Fname = (String)reader["fname"];
                answer.Lname = (String)reader["lname"];
                answer.Email = (String)reader["email"];
                answer.Phone = (String)reader["phone"];

                result.Add(answer);
            }



            manager.Close();
            return result;
        }
    }
}
