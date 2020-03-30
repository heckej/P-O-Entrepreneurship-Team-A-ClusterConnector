using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ClusterAPI.Models
{
    public class MatchQuestionModel
    {
        public int question_id;
        public MatchQuestionModelInfo[] possible_matches;
        public int msg_id;
    }

    public class MatchQuestionModelInfo
    {
        public int question_id;
        public float prob;
    }
}