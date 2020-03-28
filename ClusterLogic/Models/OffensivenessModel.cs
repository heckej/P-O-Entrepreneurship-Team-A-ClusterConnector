using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ClusterLogic.Models
{
    [Serializable]
    public class OffensivenessModel : BaseModel
    {
        private int _question_id = -1;
        private float _prob = -1;
        private int _msg_id = -1;

        public int question_id { get => _question_id; set => _question_id = value; }
        public float prob { get => _prob; set => _prob = value; }
        public int msg_id { get => _msg_id; set => _msg_id = value; }

        public bool IsComplete()
        {
            return _question_id != -1 && _prob != -1 && _msg_id != -1;
        }
    }
}