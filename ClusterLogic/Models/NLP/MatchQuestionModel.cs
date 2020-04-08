using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ClusterLogic.Models
{
    [Serializable]
    public class MatchQuestionModelRequest : BaseModel
    {
        private String _action = null;
        private int _question_id = -1;
        private String _question = null;
        private NLPQuestionModelInfo[] _compare_questions = null;
        private int _msg_id = -1;

        public int question_id { get => _question_id; set => _question_id = value; }
        public NLPQuestionModelInfo[] compare_questions { get => _compare_questions; set => _compare_questions = value; }
        public int msg_id { get => _msg_id; set => _msg_id = value; }
        public string question { get => _question; set => _question = value; }
        public string action { get => _action; set => _action = value; }

        public bool IsComplete()
        {
            return compare_questions != null && _question_id != -1 && _msg_id != -1 && _question != null && _action != null;
        }
    }

    [Serializable]
    public class NLPQuestionModelInfo : BaseModel
    {
        private int _question_id = -1;
        private String _question = null;

        public int question_id { get => _question_id; set => _question_id = value; }
        public string question { get => _question; set => _question = value; }

        public bool IsComplete()
        {
            return question_id != -1 && question != null;
        }
    }

    [Serializable]
    public class MatchQuestionModelResponse : BaseModel
    {
        private int _question_id = -1;
        private MatchQuestionModelInfo[] _possible_matches = null;
        private int _msg_id = -1;

        public int question_id { get => _question_id; set => _question_id = value; }
        public MatchQuestionModelInfo[] possible_matches { get => _possible_matches; set => _possible_matches = value; }
        public int msg_id { get => _msg_id; set => _msg_id = value; }

        public bool IsComplete()
        {
            return possible_matches != null && _question_id != -1 && _msg_id != -1;
        }
    }

    [Serializable]
    public class MatchQuestionModelInfo : BaseModel
    {
        
        private int _question_id = -1;
        private float _prob = -1;

        public int question_id { get => _question_id; set => _question_id = value; }
        public float prob { get => _prob; set => _prob = value; }

        public bool IsComplete()
        {
            return _question_id != -1 && prob != -1;
        }
    }
}