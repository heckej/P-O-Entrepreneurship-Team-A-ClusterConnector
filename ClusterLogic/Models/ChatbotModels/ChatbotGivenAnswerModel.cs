using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterLogic.Models.ChatbotModels
{
    /// <summary>
    /// based on:
    ///     We sturen jullie het antwoord op een bepaalde vraag. We maken koppeling met een endpoint van jullie API en 
    ///     versturen een JSON met 1 element met als parameters {Answer, QuestionID}. We verwachten geen speciale respons
    ///     (hoogstens gewoon een statuscode ofzo)
    ///     
    /// </summary>
    public class ChatbotGivenAnswerModel : BaseModel
    {
        private String _answer = null;
        private int _question_id = -1;

        public String answer { get => _answer; set => _answer = value; }
        public int question_id { get => _question_id; set => _question_id = value; }

        public bool IsComplete()
        {
            return _answer != null && question_id != -1;
        }
    }
}
