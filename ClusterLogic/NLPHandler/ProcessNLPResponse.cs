using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClusterLogic.Models;
using ClusterConnector.Manager;

namespace ClusterLogic.NLPHandler
{
    public class ProcessNLPResponse
    {
        // The threshold to find question matches.
        private static double MatchThreshold { get; } = 0.75;

        /// <summary>
        /// Logica: krijgt MatchedQuestionsModel binnen
        /// en beslist of er een goede match is + het antwoord op deze vraag
        /// 
        /// </summary>
        /// <param name="matchQuestionModels"></param>
        /// <returns>
        /// 
        /// Resultaat bij Bernd is dan
        /// 1) Er is een goede match
        /// 2) We moeten verderZoeken
        /// 3) Er is geen match en vragen zijn op
        /// 
        /// Ik laat aan jullie om te beslissen hoe dit 1 model / meerdere modellen eruit zullen zien
        /// return model; aan het functie is het enige dat ik nodig heb :)
        /// 
        /// </returns>
        public static MatchQuestionLogicResponse ProcessNLPMatchQuestionsResponse(MatchQuestionModelResponse matchQuestionModel)
        {
            // Create a "no match" response
            MatchQuestionLogicResponse nullResponse = new MatchQuestionLogicResponse();

            if (matchQuestionModel == null)
            {
                return nullResponse;
            }

            foreach (MatchQuestionModelInfo info in matchQuestionModel.possible_matches)
            {
                if (info.prob > MatchThreshold)
                {
                    // Query answer to matched string
                    // ! feedback on the answer is not yet considered in the query !
                    List<DBQuestion> result = new List<DBQuestion>();
                    DBManager manager = new DBManager(false); //this false 
                    String sqlCommand = $"Select answer from Answers as a and Questions as q where q.question_id == {info.question_id} and q.answer_id == a.answer_id";
                    var reader = manager.Read(sqlCommand);

                    while (reader.Read()) //reader.Read() reads entries one-by-one for all matching entries to the query
                    {
                        DBQuestion answer = new DBQuestion();
                        answer.answer = (String)reader["answer"];

                        result.Add(answer);
                    }
                    manager.Close(); //IMPORTANT! Should happen automatically, but better safe than sorry.

                    return new MatchQuestionLogicResponse(matchQuestionModel.question_id, matchQuestionModel.msg_id, info.question_id, true, result.First);
                }
            }

            return nullResponse;
        }

        public static Object ProcessNLPOffensivenessResponse(List<OffensivenessModelResponse> offensivenessModels)
        {

            return null;
        }

        public static Object ProcessNLPNonsenseResponse(List<NonsenseModelResponse> nonsenseModelResponses)
        {
            NonsenseModelResponse nonsenseModelResponse = nonsenseModelResponses[0];
            if (nonsenseModelResponse.nonsense) //if is nonsense
            {
                return null;
            }
            else
            {
                return new OffensivenessModelRequest(nonsenseModelResponse); //This gives the Server the correct response to make it known what to do next. As a simple example
            }
        }
    }
}
