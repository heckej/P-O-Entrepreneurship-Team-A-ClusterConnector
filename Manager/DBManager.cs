using ClusterConnector.Models.Database;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterConnector.Manager
{
    public class DBManager
    {
        private readonly bool keep_open = false;
        private SqlConnection sqlConnection;

        public DBManager() : this(false)
        {
        }

        public DBManager(bool keep_open)
        {
            this.keep_open = keep_open;
        }

        public SqlDataReader Read(String sqlCommand)
        {
            try
            {
                if (sqlConnection != null && sqlConnection.State == System.Data.ConnectionState.Closed)
                {
                    throw new Exception("SqlConnection is closed! @DBManager");
                }

                if (sqlConnection == null)
                {
                    sqlConnection = new SqlConnection(ServerUtilities.SQLSource);
                }

                SqlCommand command = new SqlCommand(sqlCommand, sqlConnection);
                if(command.Connection.State != System.Data.ConnectionState.Open)
                    command.Connection.Open();
                command.ExecuteNonQuery();

                if (!this.keep_open)
                {
                    sqlConnection.Close();
                }

                return command.ExecuteReader();
            }
            catch (Exception e)
            {
                TextWriter errorWriter = Console.Error;
                errorWriter.WriteLine(e.Message);
                System.Diagnostics.Debug.WriteLine(e);
                System.Diagnostics.Debug.WriteLine("Query: " + sqlCommand);
                return null;
            }
        }

        public void Close()
        {
            if (sqlConnection != null && sqlConnection.State != System.Data.ConnectionState.Closed)
            {
                sqlConnection.Close();
            }
        }
    }
}
