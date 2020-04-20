using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClusterLogic.Models;
using ClusterConnector.Manager;
using ClusterConnector.Manager;
using ClusterConnector.Models.Database;
using System.Data.SqlClient;

namespace ClusterLogic.NLPHandler
{
    public class ProcessNLPResponse
    {
        /// <summary>
        /// The threshold to find question matches.
        /// </summary>
        private static double MatchThreshold { get; } = 0.75;

        /// <summary>
        /// The threshold to find offensive sentences.
        /// </summary>
        private static double OffensiveThreshold { get; } = 0.8;

        /// <summary>
        /// Process an NLPMatchQuestionResponse and turn it into a MatchQuestionLogicResponse containing an answer,
        /// if any.
        /// </summary>
        /// <param name="matchQuestionModel">The NLP model to process.</param>
        /// <returns>A MatchQuestionLogicResponse containing either 1) nothing if there is no match or 2) the best match if there
        /// is a match.</returns>
        public static MatchQuestionLogicResponse ProcessNLPMatchQuestionsResponse(MatchQuestionModelResponse matchQuestionModel)
        {
            // Create a "no match" response
            MatchQuestionLogicResponse nullResponse = new MatchQuestionLogicResponse();

            // Check to see whether there is at least a valid answer given
            if (matchQuestionModel == null || 
                ! matchQuestionModel.IsComplete())
            {
                return nullResponse;
            }

            MatchQuestionModelInfo bestInfo = null;
            double bestMatch = 0.0;

            // Find the best match above the threshold
            foreach (MatchQuestionModelInfo info in matchQuestionModel.possible_matches)
            {
                if (info.prob > MatchThreshold && info.prob > bestMatch)
                {
                    bestInfo = info;
                    bestMatch = bestInfo.prob;
                }
            }

            // Get the answer of the best match, if any
            if (bestInfo != null)
            {
                DBManager manager = new DBManager(false); //this false 

                // Initialize the result
                MatchQuestionLogicResponse result = null;

                StringBuilder sb = new StringBuilder();
                sb.Append("SELECT answer ");
                sb.Append("FROM Answers a, Questions q ");
                sb.Append($"WHERE q.question_id = {bestInfo.question_id} AND q.answer_id = a.answer_id; ");
                String sql = sb.ToString();

                using (SqlDataReader reader = manager.Read(sql))
                {
                    // This query should only return 0 or 1 result
                    if (reader.Read())
                    {
                        result = new MatchQuestionLogicResponse(matchQuestionModel.question_id, matchQuestionModel.msg_id, bestInfo.question_id, true, reader.GetString(0));
                    }
                }

                // Close the connection
                manager.Close();

                if (result != null)
                {
                    return result;
                }
            }

            // No valid match was found, so "no answer" is returned.
            return nullResponse;
        }

        /// <summary>
        /// Process an NLP OffensivenessModelResponse and turn it into an OffensivenessLogicResponse.
        /// </summary>
        /// <param name="offensivenessModel">The NLP model to process.</param>
        /// <returns>An OffensivenessLogicResponse describing the processed OffensivenessModelResponse. The response
        /// is either descriptive of the processed response or "not complete" if the given response was invalid.</returns>
        public static OffensivenessLogicResponse ProcessNLPOffensivenessResponse(OffensivenessModelResponse offensivenessModel)
        {
            // Create an "invalid model responses" response
            OffensivenessLogicResponse nullResponse = new OffensivenessLogicResponse();

            // Check to see whether there is at least a valid answer given
            if (offensivenessModel == null ||
                !offensivenessModel.IsComplete())
            {
                return nullResponse;
            }

            

            // Decide whether the given question is offensive
            bool offensive = false;

            if (offensivenessModel.prob > OffensiveThreshold)
            {
                offensive = true;
            }

            // Check if the sentence contains a blacklisted word
            DBManager manager = new DBManager(false); //this false 
            String sentence = offensivenessModel.question;
            String[] words = sentence.Split(' ');
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT forbidden_word");
            sb.Append("FROM Blacklist");
            String sql = sb.ToString();
            List<String> blacklist = new List<String>();
            // ToDo: make query to get blacklist and put result in placklist variable
            using (SqlDataReader reader = manager.Read(sql))
            {
                // This query should only return 0 or 1 result
                while (reader.Read())
                {
                    blacklist.Add(reader.GetString(0));
                }
            }
            foreach(String word in words)
            {
                foreach(String offensiveWord in blacklist)
                {
                    if(String.Equals(word, offensiveWord))
                    {
                        offensive = true;
                        break;
                    }
                }
            }

            // Return the result
            return new OffensivenessLogicResponse(
                offensivenessModel.question_id,
                offensive,
                offensivenessModel.question,
                offensivenessModel.msg_id
                );
        }

        /// <summary>
        /// Process an NLP NonsenseModelResponse and turn it into an NonsenseLogicResponse.
        /// </summary>
        /// <param name="nonsenseModelResponse">The NLP model to process.</param>
        /// <returns>An NonsenseLogicResponse describing the processed NonsenseModelResponse. The response
        /// is either descriptive of the processed response or "not complete" if the given response was invalid.</returns>
        public static NonsenseLogicResponse ProcessNLPNonsenseResponse(NonsenseModelResponse nonsenseModelResponse)
        {
            // Create an "invalid model responses" response
            // Check to see whether there is at least a valid answer given
            NonsenseLogicResponse nullResponse = new NonsenseLogicResponse();

            if (nonsenseModelResponse == null ||
                !nonsenseModelResponse.IsComplete())
            {
                return nullResponse;
            }

            // Decide whether the given question is nonsense
            bool nonsense = false;

            if (nonsenseModelResponse.nonsense)
            {
                nonsense = true;
            }

            // Return the result
            return new NonsenseLogicResponse(
                nonsenseModelResponse.question_id,
                nonsense,
                nonsenseModelResponse.question,
                nonsenseModelResponse.msg_id
                );
        }
    }
}
