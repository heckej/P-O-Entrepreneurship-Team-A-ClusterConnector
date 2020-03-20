using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Data.SqlClient;

namespace ClusterConnector.Models
{
    public class TestQuestion
    {
        private int _id;
        public int Id { get => _id; set => _id = value; }

        public TestQuestion(int _id)
        {
            this.Id = _id;
        }

        //public static void TestConnection()
        //{
        //    using (SqlConnection connection = new SqlConnection("Data Source=clusterbot.database.windows.net;Initial Catalog=Cluster;User ID=BerndWeckx;Password=Bubbi100@3751;Connect Timeout=30;Encrypt=True;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False"))
        //    {
        //        SqlCommand command = new SqlCommand("Select * From dbo.TestTable", connection);
        //command.Connection.Open();
        //        command.ExecuteNonQuery();

        //        SqlDataReader dataReader = command.ExecuteReader();
        //        while (dataReader.Read())
        //        {
        //            Console.WriteLine(dataReader["ID"]); //Alternate by collumn index datareader.getValue(0) for collumn at 0 index
        //        }
        //    }
        //}


        //public static void TestConnection(List<int> numbers)
        //{
        //    String insertString = "";
        //    numbers.ForEach(num => insertString += "("+num + "),");
        //    insertString = insertString.Substring(0,insertString.Length - 1);
        //    using (SqlConnection connection = new SqlConnection("Data Source=clusterbot.database.windows.net;Initial Catalog=Cluster;User ID=BerndWeckx;Password=Bubbi100@3751;Connect Timeout=30;Encrypt=True;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False"))
        //    {
        //        SqlCommand command = new SqlCommand("Insert Into dbo.TestTable Values "+insertString, connection);
        //        command.Connection.Open();
        //        command.ExecuteNonQuery();
        //    }
        //}

        public static List<TestQuestion> TestConnection(int id)
        {
            List<TestQuestion> questions = new List<TestQuestion>();
            using (SqlConnection connection = new SqlConnection("Data Source=clusterbot.database.windows.net;Initial Catalog=Cluster;User ID=BerndWeckx;Password=Bubbi100@3751;Connect Timeout=30;Encrypt=True;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False"))
            {
                String sqlCommand = "Select * From dbo.TestTable test Where test.ID = " + id +";";
                SqlCommand command = new SqlCommand(sqlCommand, connection);
                command.Connection.Open();
                command.ExecuteNonQuery();

                SqlDataReader dataReader = command.ExecuteReader();
                while (dataReader.Read())
                {
                    //Console.WriteLine(dataReader["ID"]); //Alternate by collumn index datareader.getValue(0) for collumn at 0 index
                    questions.Add(new TestQuestion((int)dataReader["ID"]));
                }
            }

            return questions;
        }
    }
}
