﻿using System;
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

    /// <summary>
    /// An NLP MatchQuestionModelResponse processed by the Cluster Logic.
    /// </summary>
    [Serializable]
    public class MatchQuestionLogicResponse : BaseModel
    {
        /// <summary>
        /// The id of the matched question.
        /// </summary>
        public int Question_id { get; } = -1;

        /// <summary>
        /// The id of the best match.
        /// </summary>
        public int Match_id { get; } = -1;

        /// <summary>
        /// True if and only if the question has a match.
        /// </summary>
        public bool Match { get; } = false;

        /// <summary>
        /// Create a new MatchQuestionLogicResponse.
        /// </summary>
        /// <param name="question_id">The id of the matched question (>-1 if existant).</param>
        /// <param name="match_id">The id of the best match (>-1 if existant).</param>
        /// <param name="match">True if and only if there is a match.</param>
        public MatchQuestionLogicResponse(int question_id = -1, int match_id = -1, bool match = false)
        {
            this.Question_id = question_id;
            this.Match_id = match_id;
            this.Match = match;
        }

        /// <summary>
        /// Check whether this response has a match.
        /// </summary>
        /// <returns></returns>
        public bool HasMatch()
        {
            return this.Match;
        }

        /// <summary>
        /// Return true if and only if this response is complete, e.g. it has a match and the ids are positive.
        /// </summary>
        /// <returns></returns>
        public bool IsComplete()
        {
            return Match && Question_id > -1 && Match_id > -1;
        }
    }
}