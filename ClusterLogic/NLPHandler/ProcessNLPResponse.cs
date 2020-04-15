using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClusterLogic.Models;

namespace ClusterLogic.NLPHandler
{
    public class ProcessNLPResponse
    {
        /// <summary>
        /// Logica: krijgt MatchedQuestionsModel binnen
        /// en beslist of er een goede match is + het antwoord op deze vraag
        /// 
        /// </summary>
        /// <param name="matchQuestionModels"></param>
        /// <returns></returns>
        public static Object ProcessNLPMatchQuestionsResponse(List<MatchQuestionModelResponse> matchQuestionModels)
        {

            return null;
        }

        public static Object ProcessNLPOffensivenessResponse(List<OffensivenessModelResponse> offensivenessModels)
        {

            return null;
        }

        public static Object ProcessNLPNonsenseResponse(List<NonsenseModelResponse> nonsenseModelResponses)
        {
            if (nonsenseModelResponses[0].nonsense) //if is nonsense
            {
                return null;
            }
            else
            {
                return new OffensivenessModelRequest(nonsenseModelResponses[0]); //This gives the Server the correct response to make it known what to do next. As a simple example
            }
        }
    }
}
