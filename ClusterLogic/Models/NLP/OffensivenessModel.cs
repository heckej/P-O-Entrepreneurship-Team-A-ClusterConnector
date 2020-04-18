using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ClusterLogic.Models.ChatbotModels;

namespace ClusterLogic.Models
{
    [Serializable]
    public class OffensivenessModelRequest : BaseModel
    {
        private String _action = null;
        private int _question_id = -1;
        private String _question = null;
        private int _msg_id = -1;

        public OffensivenessModelRequest() { }

        public OffensivenessModelRequest(ChatbotGivenAnswerModel chatbotGivenAnswerModel)
        {
            this.question = chatbotGivenAnswerModel.answer;
            this.question_id = chatbotGivenAnswerModel.question_id;
            this.msg_id = -1; //TODO: what should this be?
            this.action = "IS_NONSENSE";
        }

        public OffensivenessModelRequest(NonsenseModelResponse nonsenseModelResponse)
        {
            this.msg_id = nonsenseModelResponse.msg_id;
            this.question = nonsenseModelResponse.question;
            this.question_id = nonsenseModelResponse.question_id;
            this.action = "ESTIMATE_OFFENSIVENESS";
        }

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
            return question != null && _question_id != -1 && _prob != -1 && _msg_id != -1;
        }
    }

    /// <summary>
    /// An NLP OffensivenessModelResponse processed by the Cluster logic.
    /// </summary>
    [Serializable]
    public class OffensivenessLogicResponse : BaseModel
    {
        /// <summary>
        /// The id of the checked question.
        /// </summary>
        public int Question_id { get; } = -1;

        /// <summary>
        /// A boolean indicating whether the checked question is offensive.
        /// </summary>
        public bool Offensive { get; } = false;

        /// <summary>
        /// The checked question.
        /// </summary>
        public string Question { get; } = null;

        /// <summary>
        /// The id of the processed message.
        /// </summary>
        public int Msg_id { get; } = -1;

        /// <summary>
        /// Check whether the processed question is offensive.
        /// </summary>
        /// <returns>True if and only if the processed question is offensive.</returns>
        public bool IsOffensive()
        {
            return Offensive;
        }

        /// <summary>
        /// Return true if and only if the ids are positive and the question is not null or empty.
        /// </summary>
        /// <returns>true if and only if the ids are positive and the question is not null or empty.</returns>
        public bool IsComplete()
        {
            return Question_id > -1 && !String.IsNullOrEmpty(Question) && Msg_id > -1;
        }

        /// <summary>
        /// Create a new processed offensiveness response from the logic.
        /// </summary>
        /// <param name="questionId">The id of the checked question.</param>
        /// <param name="offensive">A boolean indicating whether the checked question is offensive (= true) or not (= false).</param>
        /// <param name="question">The checked question.</param>
        /// <param name="msgId">The id of the processed message.</param>
        public OffensivenessLogicResponse(int questionId = -1, bool offensive = false, string question = null, int msgId = -1)
        {
            this.Question_id = questionId;
            this.Offensive = offensive;
            this.Question = question;
            this.Msg_id = msgId;
        }

    }

    [Serializable]
    public class NonsenseModelResponse : BaseModel
    {
        private int _question_id = -1;
        private bool _nonsense = false;
        private int _msg_id = -1;
        private String _question = null;

        public int question_id { get => _question_id; set => _question_id = value; }
        public int msg_id { get => _msg_id; set => _msg_id = value; }
        public bool nonsense { get => _nonsense; set => _nonsense = value; }
        public string question { get => _question; set => _question = value; }

        public bool IsComplete()
        {
            return question != null && _question_id != -1 && _msg_id != -1;
        }
    }
}