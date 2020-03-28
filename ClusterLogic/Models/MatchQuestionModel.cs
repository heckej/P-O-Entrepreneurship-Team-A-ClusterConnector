using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ClusterLogic.Models
{
    [Serializable]
    public class MatchQuestionModel : BaseModel
    {
        private int _question_id;
        private MatchQuestionModelInfo[] _possible_matches;
        private int _msg_id;

        public int question_id { get => _question_id; set => _question_id = value; }
        public MatchQuestionModelInfo[] possible_matches { get => _possible_matches; set => _possible_matches = value; }
        public int msg_id { get => _msg_id; set => _msg_id = value; }
    }

    [Serializable]
    public class MatchQuestionModelInfo : BaseModel
    {
        
        private int _question_id;
        private float _prob;

        public int question_id { get => _question_id; set => _question_id = value; }
        public float prob { get => _prob; set => _prob = value; }
    }
}