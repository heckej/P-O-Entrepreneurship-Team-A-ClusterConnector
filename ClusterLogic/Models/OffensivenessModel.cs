using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ClusterLogic.Models
{
    [Serializable]
    public class OffensivenessModel : BaseModel
    {
        private int _question_id;
        private float _prob;
        private int _msg_id;

        [Must]
        public int question_id { get => _question_id; set => _question_id = value; }
        public float prob { get => _prob; set => _prob = value; }
        public int msg_id { get => _msg_id; set => _msg_id = value; }
    }
}