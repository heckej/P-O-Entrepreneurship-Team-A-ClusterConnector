using ClusterConnector.Manager;
using ClusterConnector.Models.Database;
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

        /// <summary>
        /// 
        /// TODO: Select all ANSWERED questions put them in the MatchQuestionModelRequest model
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static MatchQuestionModelRequest ProcessChatbotReceiveQuestion(List<ChatbotNewQuestionModel> list)
        {
            MatchQuestionModelRequest mqmr = new MatchQuestionModelRequest();

            //Example on how to turn a Query String into data from the SQL database

            List<DBQuestion> result = new List<DBQuestion>();
            DBManager manager = new DBManager(false); //this false 
            String sqlCommand = "Select * From dbo.Questions answer Where ";

            for (int i = 0; i < 10; i++)
            {
                if (i != 10 - 1)
                {
                    sqlCommand += "answer.question_id = " + i + " OR ";
                }
                else
                {
                    sqlCommand += "answer.question_id = " + i + ";";
                }
            }
            var reader = manager.Read(sqlCommand);

            while (reader.Read()) //reader.Read() reads entries one-by-one for all matching entries to the query
            {
                //
                //reader["xxx"] where 'xxx' is the collumn name of the particular table you get as result from the query.
                //You get these values a generic 'Object' so typecasting to the proper value should be safe. eg. (int)reader["xxx"]
                //
               

                DBQuestion answer = new DBQuestion();
                answer.Question_id = (int)reader["question_id"];
                answer.Question = (String)reader["question"];
                answer.Answer_id = (int)reader["answer_id"];

                result.Add(answer);
            }
            manager.Close(); //IMPORTANT! Should happen automatically, but better safe than sorry.


            //**********************************
            mqmr.action = "MATCH_QUESTIONS"; //Standard
            mqmr.question = list[0].question;
            mqmr.msg_id = 0; //Currently not really used
            mqmr.question_id = 0; //This id does not exist at this point

            List<NLPQuestionModelInfo> comparisonQuestions = new List<NLPQuestionModelInfo>();
            for (int i = 0; i < result.Count; i++)
            {
                comparisonQuestions.Add(new NLPQuestionModelInfo() { question = result[i].Question, question_id = result[i].Question_id });
            }
            mqmr.compare_questions = comparisonQuestions.ToArray();

            return mqmr;
        }

        /// <summary>
        /// Perform NLP Nonsense check
        /// </summary>
        /// <param name="list"></param>
        public static OffensivenessModelRequest ProcessChatbotReceiveAnswer(List<ChatbotGivenAnswerModel> list)
        {
            return new OffensivenessModelRequest(list[0]);
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
