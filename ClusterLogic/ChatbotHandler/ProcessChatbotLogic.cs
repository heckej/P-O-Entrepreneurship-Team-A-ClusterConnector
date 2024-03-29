﻿using ClusterConnector;
using ClusterConnector.Manager;
using ClusterConnector.Models.Database;
using ClusterLogic.Models;
using ClusterLogic.Models.ChatbotModels;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
        public static List<ChatbotNewAnswerModel> CheckAndGetNewAnswers(string userID = null, int questionID = -1)
        {
            // connect to database and define query
            DBManager manager = new DBManager(true);
            String query = "Select q.question_id, q.question, q.answer_id, a.answer " +
                           "from dbo.Answers a, dbo.Questions q " +
                           $"where q.question_id = {questionID} and q.answer_id = a.answer_id;";// and " +
                           //"a.approved = 'true';";

            // execute query and read answer
            DBQuestion sqlResult = null;
            using (SqlDataReader reader = manager.Read(query))
            {
                if (reader.Read())
                {
                    sqlResult = new DBQuestion();
                    sqlResult.Question_id = (int)reader["question_id"];
                    sqlResult.Question = (String)reader["question"];
                    sqlResult.Answer_id = (int)reader["answer_id"];
                    sqlResult.Answer = (String)reader["answer"];
                    /****/
                    sqlResult.Answer = ServerUtilities.SQLSafeToUserInput(sqlResult.Answer);
                    sqlResult.Question = ServerUtilities.SQLSafeToUserInput(sqlResult.Question);
                    /****/
                }
            }

            // Close the connection
            manager.Close();

            if (sqlResult == null)
            {
                return new List<ChatbotNewAnswerModel>()
                {
                    new ChatbotNewAnswerModel(userID, null, questionID, -1, null, -1, 0)
                };
            }

            // Return new output-model
            return new List<ChatbotNewAnswerModel>()
            {
                new ChatbotNewAnswerModel(userID, sqlResult.Question, sqlResult.Question_id, -1 /**no temporary chatbot id needed*/,
                                            sqlResult.Answer, sqlResult.Answer_id, 1 /**Where is the certainty found?*/)
            };
        }


        /// <summary>
        /// Get all the answered questions and wrap them into a MatchQuestionModelRequest.
        /// </summary>
        /// <param name="list">A list of ChatbotNewQuestionModels to process.</param>
        /// <returns>A MatchQuestionModelRequest containing all the answered questions from the forum.</returns>
        public static MatchQuestionModelRequest ProcessChatbotReceiveQuestion(List<ChatbotNewQuestionModel> list)
        {
            MatchQuestionModelRequest mqmr = new MatchQuestionModelRequest();

            //Example on how to turn a Query String into data from the SQL database

            List<DBQuestion> result = new List<DBQuestion>();
            DBManager manager = new DBManager(true);

            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT * ");
            sb.Append("FROM Questions q ");
            sb.Append("WHERE q.answer_id IS NOT NULL; ");
            String sqlCommand = sb.ToString();

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

                /****/
                answer.Question = ServerUtilities.SQLSafeToUserInput(answer.Question);
                /****/

                result.Add(answer);
            }
            manager.Close(); //IMPORTANT! Should happen automatically, but better safe than sorry.


            //**********************************
            mqmr.action = "MATCH_QUESTIONS".ToLower(); //Standard
            mqmr.question = list[0].question;
            mqmr.msg_id = 0; //Currently not really used
            mqmr.question_id = 0; //This id does not exist at this point

            List<NLPQuestionModelInfo> comparisonQuestions = new List<NLPQuestionModelInfo>();
            for (int i = 0; i < result.Count; i++)
            {
                comparisonQuestions.Add(new NLPQuestionModelInfo() { question = result[i].Question, question_id = result[i].Question_id });
            }
            mqmr.compare_questions = comparisonQuestions.ToArray();

            // IMPORTANT: DO NOT REMOVE THIS LINE
            mqmr.msg_id = ClusterConnector.ServerUtilities.getAndGenerateMsgID(list[0].chatbot_temp_id,list[0].question,list[0].user_id); 

            return mqmr;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="list"></param>
        /// <returns> This functions should have a response, if the response == null, then no response will be given through the websocket</returns>
        public static List<ChatbotAnswerRequestResponseModel> ProcessChatbotRequestAnswerToQuestion(List<ChatbotAnswerRequestModel> list)
        {
            ChatbotAnswerRequestModel answerRequest = list[0];

            int question_id = answerRequest.question_id;

            // connect to database and define query
            DBManager manager = new DBManager(true); //this false 
            String query = "Select a.answer_id, a.answer " +
                           "from dbo.Answers as a, dbo.Questions as q " +
                           $"where q.question_id = {question_id} and q.answer_id = a.answer_id ";
                           // "+ and a.approved == true;";

            // execute query and read answer
            DBQuestion sqlResult = null;
            using (SqlDataReader reader = manager.Read(query))
            {
                while (reader.Read())
                {
                    sqlResult = new DBQuestion();
                    sqlResult.Answer_id = (int)reader["answer_id"];
                    sqlResult.Answer = (String)reader["answer"];

                    /****/
                    sqlResult.Answer = ServerUtilities.SQLSafeToUserInput(sqlResult.Answer);
                    /****/
                }
            }

            // Close the connection
            manager.Close();

            if (sqlResult == null){
                return null;
            }

            return new List<ChatbotAnswerRequestResponseModel>()
            {
                new ChatbotAnswerRequestResponseModel(answerRequest.user_id, sqlResult.Answer_id, question_id, true, sqlResult.Answer)
            };
        }

        /// <summary>
        ///     TODO: What should the socket response be when there are no unanswered questions?
        /// </summary>
        /// <param name="list"></param>
        /// <returns> This functions should have a response, if the response == null or empty, then no response will be given through the websocket</returns>
        public static List<ChatbotRequestUnansweredQuestionsResponseModel> ProcessChatbotRequestAnswerToQuestion(List<ChatbotRequestUnansweredQuestionsModel> list)
        {
            // What should this function do? --> see function below
            throw new NotImplementedException();
        }


        /// <summary>
        /// Function to get a number of open questions from the database
        /// </summary>
        /// <param name="nbQuestions"></param>
        /// <param name="user_id"></param>
        /// <returns> Returns a model with a list of unanswered questions </returns>
        public static ChatbotResponseUnansweredQuestionsModel RetrieveOpenQuestions(string user_id = null, int nbQuestions = 3)
        {
            List<DBQuestion> result = new List<DBQuestion>();
            DBManager manager = new DBManager(true); //this false 

            StringBuilder sb = new StringBuilder();
            sb.Append($"SELECT TOP {nbQuestions} q.question_id, q.question ");
            sb.Append("FROM Questions q ");
            sb.Append("WHERE q.answer_id is NULL");
            String sqlCommand = sb.ToString();

            var reader = manager.Read(sqlCommand);

            while (reader.Read()) //reader.Read() reads entries one-by-one for all matching entries to the query
            {
                DBQuestion answer = new DBQuestion();
                answer.Question_id = (int)reader["question_id"];
                answer.Question = (String)reader["question"];

                /****/
                answer.Question = ServerUtilities.SQLSafeToUserInput(answer.Question);
                /****/
                result.Add(answer);
            }
            manager.Close(); //IMPORTANT! Should happen automatically, but better safe than sorry.

            List<ChatbotQuestionHasNoAnswerModel> openQuestions = new List<ChatbotQuestionHasNoAnswerModel>();
            for (int i = 0; i < result.Count; i++)
            {
                openQuestions.Add(new ChatbotQuestionHasNoAnswerModel(result[i].Question, result[i].Question_id));
            }

            return new ChatbotResponseUnansweredQuestionsModel(openQuestions.ToArray(), user_id);
        }

        public static OffensivenessModelRequest ProcessChatbotReceiveAnswer(ChatbotGivesAnswerModelToServer item, ChatbotGivesAnswersToQuestionsToServer chatbotGivesAnswersToQuestionsToServer)
        {
            return new OffensivenessModelRequest(item, chatbotGivesAnswersToQuestionsToServer);
        }

        /// <summary>
        /// This method does all the things necessary to handle a offensive answer given by a certain user
        /// --> Answer is added to bad_answers table
        /// </summary>
        /// <param name="newAnswerNonsenseCheck"></param>
        public static void ProcessOffensiveAnswer(NewAnswerOffenseCheck newAnswerOffenseCheck)
        {
            DBManager manager = new DBManager(true);
            /****/
            String sqlSafeAnswer = ServerUtilities.UserInputToSQLSafe(newAnswerOffenseCheck.answer);
            /****/

            // Add a reference to the answer in the bad_answer table
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO dbo.BadAnswers (bad_answer, question_id, answer_author_id) ");
            sb.Append($"VALUES ('{sqlSafeAnswer}', {newAnswerOffenseCheck.question_id}, '{newAnswerOffenseCheck.user_id}') ");
            String sqlCommand = sb.ToString();

            manager.Read(sqlCommand);

            manager.Close();
        }


        /// <summary>
        /// This method does all the things necessary to handle a nonsense answer given by a certain user
        /// </summary>
        /// <param name="newAnswerNonsenseCheck"></param>
        public static void ProcessNonsenseAnswer(NewAnswerNonsenseCheck newAnswerNonsenseCheck)
        {
            ProcessOffensiveAnswer(new NewAnswerOffenseCheck(newAnswerNonsenseCheck)); // Just adds answer to bad_answer table 
        }

        /// <summary>
        /// Get all the unanswered questions and wrap them into a MatchQuestionModelRequest.
        /// </summary>
        /// <param name="list">A list of ChatbotNewQuestionModels to process.</param>
        /// <returns>A MatchQuestionModelRequest containing all the answered questions from the forum.</returns>
        public static MatchQuestionModelRequest GenerateModelCompareToOpenQuestions(NewQuestion newQuestion)
        {
            MatchQuestionModelRequest mqmr = new MatchQuestionModelRequest();

            List<DBQuestion> result = new List<DBQuestion>();
            DBManager manager = new DBManager(true);

            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT * ");
            sb.Append("FROM Questions q ");
            sb.Append("WHERE q.answer_id IS NOT NULL; ");
            String sqlCommand = sb.ToString();

            var reader = manager.Read(sqlCommand);

            while (reader.Read())
            {
                DBQuestion answer = new DBQuestion();
                answer.Question_id = (int)reader["question_id"];
                answer.Question = (String)reader["question"];
                answer.Answer_id = (int)reader["answer_id"];

                /****/
                answer.Question = ServerUtilities.SQLSafeToUserInput(answer.Question);
                /****/

                result.Add(answer);
            }
            manager.Close(); //IMPORTANT! Should happen automatically, but better safe than sorry.

            mqmr.action = "MATCH_QUESTIONS".ToLower(); //Standard
            mqmr.question = newQuestion.question;
            mqmr.question_id = -1; // This id does not exist at this point

            List<NLPQuestionModelInfo> comparisonQuestions = new List<NLPQuestionModelInfo>();
            for (int i = 0; i < result.Count; i++)
            {
                comparisonQuestions.Add(new NLPQuestionModelInfo() { question = result[i].Question, question_id = result[i].Question_id });
            }
            mqmr.compare_questions = comparisonQuestions.ToArray();


            mqmr.msg_id = ClusterConnector.ServerUtilities.getAndGenerateMsgIDOpenQuestions(newQuestion.chatbot_temp_id, newQuestion.question, newQuestion.user_id);

            return mqmr;
        }

        /// <summary>
        /// Given is an answer that is guaranteed to be nonsene, you are given the question and user id, process these.
        /// </summary>
        /// <param name="newQuestionNonsenseCheck"></param>
        public static void ProcessNonsenseQuestion(NewQuestionNonsenseCheck newQuestionNonsenseCheck)
        {
            //throw new NotImplementedException();
            return;
        }

        /// <summary>
        /// Given is an answer that is guaranteed to be offensive, you are given the question and user id, process these.
        /// </summary>
        /// <param name="newQuestionNonsenseCheck"></param>
        public static void ProcessOffensiveAnswer(NewQuestionNonsenseCheck newQuestionNonsenseCheck)
        {
            //throw new NotImplementedException();
            return;
        }

        /// <summary>
        /// This method gets called when the Server detects a new question, add this question to the database
        /// And generate a new UNIQUE id for this question and return it.
        /// </summary>
        /// <param name="openQuestion">The new question to store.</param>
        /// <returns>The new unique ID of the question to store or -1 of the program was unable to add the
        /// id to the database</returns>
        internal static int assignQuestionIdToNewQuestion(NewOpenQuestion openQuestion)
        {
            int res = -1;

            DBManager manager = new DBManager(true);

            /****/
            String sqlSafeQuestion = ServerUtilities.UserInputToSQLSafe(openQuestion.question);
            /****/

            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO dbo.Questions (question, answer_id) ");
            sb.Append($"VALUES ('{sqlSafeQuestion}', NULL); ");
            String sqlCommand = sb.ToString();

            manager.Read(sqlCommand);
            manager.Close();

            manager = new DBManager(true);

            sb = new StringBuilder();
            sb.Append("SELECT question_id ");
            sb.Append("FROM dbo.Questions ");
            sb.Append($"WHERE question = '{sqlSafeQuestion}'");
            sqlCommand = sb.ToString();

            var reader = manager.Read(sqlCommand);

            // Get the new unique id
            if (reader.Read()) // We only expect one result
            {
                res = reader.GetInt32(0);
            }

            manager.Close();

            return res;
        }

        /// <summary>
        /// This method gets called when there is a new answer to store in the database.
        /// </summary>
        /// <param name="answer">The new answer to store.</param>
        /// <returns>The new unique ID of the answer to store or -1 of the program was unable to add the
        /// id to the database.</returns>
        private static int assignAnswerIdToNewAnswer(string answer, string user_id)
        {
            int res = -1;

            if (String.IsNullOrEmpty(answer))
            {
                return res;
            }

            DBManager manager = new DBManager(true);

            /****/
            String sqlSafeAnswer = ServerUtilities.UserInputToSQLSafe(answer);
            /****/

            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO dbo.Answers (answer, user_id) ");
            sb.Append($"VALUES ('{sqlSafeAnswer}', '{user_id}'); ");
            String sqlCommand = sb.ToString();

            manager.Read(sqlCommand);

            sb = new StringBuilder();
            sb.Append("SELECT answer_id ");
            sb.Append("FROM dbo.Answers ");
            sb.Append($"WHERE answer = '{sqlSafeAnswer}'; ");
            sqlCommand = sb.ToString();

            var reader = manager.Read(sqlCommand);

            // Get the new unique id
            if (reader.Read()) // We only expect one result
            {
                res = reader.GetInt32(0);
            }

            manager.Close();

            return res;
        }

        /// <summary>
        /// This method gets called when an Open Question has just been created.
        /// 
        /// </summary>
        /// <param name="user_id">User who asked the question</param>
        /// <param name="question_id">Question ID assigned to user's question</param>
        public static void AddNewOpenAnswer(string user_id, int question_id)
        {
            DBManager manager = new DBManager(true);

            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO dbo.OpenAnswers (question_id, user_id) ");
            sb.Append($"VALUES ({question_id}, '{user_id}'); ");
            String sqlCommand = sb.ToString();

            manager.Read(sqlCommand);
            manager.Close();
        }


        /// <summary>
        /// Should return the model containing table values of OpenAnswer entry. After retrieving it, the entry from OpenAnswers with question_id is removed.
        /// </summary>
        /// <param name="question_id"></param>
        /// <returns>null if this question is not present in the OpenAnswers or the user_id of the user who asked the question otherwise.</returns>
        public static string RetrieveOpenAnswer(int question_id)
        {
            string res = null;

            DBManager manager = new DBManager(true);

            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT user_id ");
            sb.Append("FROM dbo.OpenAnswers ");
            sb.Append($"WHERE question_id = {question_id}; ");
            string sqlCommand = sb.ToString();

            var reader = manager.Read(sqlCommand);

            // Get the new unique id
            if (reader.Read()) // We only expect one result
            {
                res = reader.GetString(0);
            }

            manager.Close();

            return res;
        }

        /// <summary>
        /// Close the open answer entry. This function should be called after the actual answer has been found.
        /// </summary>
        /// <param name="question_id">The question_id of the entry to close.</param>
        public static void CloseOpenAnswer(int question_id)
        {
            DBManager manager = new DBManager(true);

            StringBuilder sb = new StringBuilder();
            sb.Append("DELETE ");
            sb.Append("FROM dbo.OpenAnswers ");
            sb.Append($"WHERE question_id = {question_id}; ");
            string sqlCommand = sb.ToString();

            manager.Read(sqlCommand);

            manager.Close();
        }

        /// <summary>
        /// This method gets called when the Server detects a new Answer to an Open Question. Add this answer to the open questions and
        /// close it.
        /// </summary>
        /// <param name="newAnswerNonsenseCheck">The model containing the answer to add, the user who wrote it, and the question_id to refer to.</param>
        public static int SaveAnswerToOpenQuestion(NewAnswerOffenseCheck newAnswerNonsenseCheck)
        {
            // Store the answer
            int ansId = assignAnswerIdToNewAnswer(newAnswerNonsenseCheck.answer, newAnswerNonsenseCheck.user_id);

            // Add a reference to the answer to the question
            DBManager manager = new DBManager(true);

            // Reference the new answer from the questions table
            StringBuilder sb = new StringBuilder();
            sb.Append("UPDATE dbo.Questions ");
            sb.Append($"SET answer_id = {ansId} ");
            sb.Append($"WHERE question_id = {newAnswerNonsenseCheck.question_id}; ");
            String sqlCommand = sb.ToString();

            manager.Read(sqlCommand);
            manager.Close();

            return ansId;
        }

        /// <summary>
        /// This function gets called when the server wants to add a new, unanswered question to the database.
        /// </summary>
        /// <param name="newQuestionNonsenseCheck">The model containing all the information about the question to add.</param>
        /// <returns>The id of the question that was just added.</returns>
        

        public static int SaveQuestionToDatabase(NewOpenQuestion serverData)
        {
            return assignQuestionIdToNewQuestion(serverData);
        }

        /// <summary>
        /// Server receives feedback for a certain answer from a certain user.
        /// </summary>
        /// <param name="chatbotFeedbackModel"></param>
        /// <returns> optional return, can be void </returns>
        public static object ProcessFeedbackAnswer(ChatbotFeedbackModel chatbotFeedbackModel)
        {
            return null;
        }
    }
}
