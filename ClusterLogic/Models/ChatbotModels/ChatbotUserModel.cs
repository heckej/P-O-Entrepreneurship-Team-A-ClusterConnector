using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterLogic.Models.ChatbotModels
{
    public class ChatbotUserModel : BaseModel
    {
        private int _user_id = -1;

        public int user_id { get => _user_id; set => _user_id = value; }

        public bool IsComplete()
        {
            return _user_id != -1;
        }
    }
}
