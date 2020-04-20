using ClusterLogic.Models;
using ClusterLogic.Models.ChatbotModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterLogic.ChatbotHandler
{
    public class ProcessChatbotLogic
    {
        /// <summary>
        /// Takes a certain userID AND/OR questionID and checks the database for new answers on possible open questions
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="questionID"></param>
        /// <returns> Returns new answers on possible open questions for a certain user or question </returns>
        public List<ChatbotNewAnswerModel> CheckAndGetNewAnswers(int userID = -1, int questionID = -1)
        {
            // IMPLEMENTATION BY LOUIS

            // connect to database and define query
            string connectionString = "Data Source=clusterbot.database.windows.net;Initial Catalog=Cluster;Persist Security Info=True;User ID=Martijn;Password=sY6WRDL2pY7qmsY3";
            String query = "Select answer from dbo.Anwers as a and dbo.Questions as q where q.question_id == '" + questionID + "' and q.answer_id == a.answer_id"
                " and a.approved == true;";

            // execute query and read answer
            List<ChatbotNewAnswerModel> result = null;
            using(var(connection = new SqlConection(connectionString)))
            {
                var reader = command.ExecuteReade();
                while (reader.Read())
                {
                    result = reader.GetString(0);
                }
                reader.Close();
            }
            connectionString.Close();
            
            return result;
        }


        public static void ProcessChatbotReceiveAnswer(List<ChatbotGivenAnswerModel> list)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="list"></param>
        /// <returns> This functions should have a response, if the response == null, then no response will be given through the websocket</returns>
        public static List<ChatbotAnswerRequestResponseModel> ProcessChatbotRequestAnswerToQuestion(List<ChatbotAnswerRequestModel> list)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     TODO: What should the socket response be when there are no unanswered questions?
        /// </summary>
        /// <param name="list"></param>
        /// <returns> This functions should have a response, if the response == null or empty, then no response will be given through the websocket</returns>
        public static List<ChatbotRequestUnansweredQuestionsResponseModel> ProcessChatbotRequestAnswerToQuestion(List<ChatbotRequestUnansweredQuestionsModel> list)
        {
            throw new NotImplementedException();
        }
    }
}
