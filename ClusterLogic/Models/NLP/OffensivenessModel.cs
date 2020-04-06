using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ClusterLogic.Models
{

    [Serializable]
    public class OffensivenessModelRequest : BaseModel
    {
        private String _action = null;
        private int _question_id = -1;
        private String _question = null;
        private int _msg_id = -1;

        public int question_id { get => _question_id; set => _question_id = value; }
        public int msg_id { get => _msg_id; set => _msg_id = value; }
        public string question { get => _question; set => _question = value; }
        public string action { get => _action; set => _action = value; }

        public bool IsComplete()
        {
            return _question_id != -1&& _msg_id != -1 && _action != null && _question != null;
        }
    }

    [Serializable]
    public class OffensivenessModelResponse : BaseModel
    {
        private int _question_id = -1;
        private float _prob = -1;
        private String _question = null;
        private int _msg_id = -1;

        public int question_id { get => _question_id; set => _question_id = value; }
        public float prob { get => _prob; set => _prob = value; }
        public int msg_id { get => _msg_id; set => _msg_id = value; }
        public string question { get => _question; set => _question = value; }

        public bool IsComplete()
        {
            return _question_id != -1 && _prob != -1 && _msg_id != -1;
        }
    }

    [Serializable]
    public class NonsenseModelResponse : BaseModel
    {
        private int _question_id = -1;
        private bool _nonsense = false;
        private int _msg_id = -1;

        public int question_id { get => _question_id; set => _question_id = value; }
        public int msg_id { get => _msg_id; set => _msg_id = value; }
        public bool nonsense { get => _nonsense; set => _nonsense = value; }

        public bool IsComplete()
        {
            return _question_id != -1 && _msg_id != -1;
        }
    }
}