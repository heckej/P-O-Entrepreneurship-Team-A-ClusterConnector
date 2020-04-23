using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClusterLogic.Models;
using ClusterConnector.Manager;
using ClusterConnector.Models.Database;
using System.Data.SqlClient;
using System.Diagnostics;

namespace ClusterLogic.NLPHandler
{
    public class ProcessNLPResponse
    {
        /// <summary>
        /// The threshold to find question matches.
        /// </summary>
        private static double MatchThreshold { get; } = 0.5;

        /// <summary>
        /// The threshold to find offensive sentences.
        /// </summary>
        private static double OffensiveThreshold { get; } = 0.6;

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

            // Collect all questions that might match
            List<MatchQuestionModelInfo> matchCandidates = new List<MatchQuestionModelInfo>();
            foreach (MatchQuestionModelInfo info in matchQuestionModel.possible_matches)
            {
                if (info.prob > MatchThreshold)
                {
                    matchCandidates.Add(info);
                }
            }

            // Call QueryHelperRecursive to get best match that also has an answer
            return QueryHelperRecursive(matchCandidates, matchQuestionModel);
        }

        /// <summary>
        /// Recursive function that looks for the best possible match that also has an aswer in the database
        /// </summary>
        /// <param name="match_candidates"></param>
        /// <param name="matchQuestionModel"></param>
        /// <returns></returns>
        private static MatchQuestionLogicResponse QueryHelperRecursive(List<MatchQuestionModelInfo> match_candidates, MatchQuestionModelResponse matchQuestionModel)
        {
            MatchQuestionModelInfo bestInfo = null;
            
            if (match_candidates.Count == 0)
            { return null; }
            else
            {
                // get best match of all candidates
                double bestMatch = 0.0;
                foreach (MatchQuestionModelInfo info in match_candidates)
                {
                    if (info.prob > bestMatch)
                    {
                        bestInfo = info;
                        bestMatch = info.prob;
                    }
                } 
            }

            if (bestInfo == null)
            { return null; }

            // Perform query to get the answer to the best match
            DBManager manager = new DBManager(true);

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

            manager.Close();

            // If a result is found, return the result, else look for another option that does have an answer
            if (result != null)
            {
                return result;
            }
            else
            {
                match_candidates.Remove(bestInfo);
                return QueryHelperRecursive(match_candidates, matchQuestionModel);
            }

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
            DBManager manager = new DBManager(true);
            String sentence = offensivenessModel.question;
            String[] words = sentence.Split(' ');
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT forbidden_word ");
            sb.Append("FROM dbo.Blacklist");
            String sql = sb.ToString();
            List<String> blacklist = new List<String>();
            // ToDo: make query to get blacklist and put result in placklist variable
            using (SqlDataReader reader = manager.Read(sql))
            {
                if (reader != null)
                    // This query should only return 0 or 1 result
                    while (reader.Read())
                    {
                        blacklist.Add(reader.GetString(0));
                    }
                else
                    Debug.WriteLine("Reader null");
            }
            manager.Close();
            foreach (String word in words)
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
