using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterLogic.Models.ChatbotModels
{
    public class ChatbotUserModel : BaseModel
    {
        private string _user_id = null;

        public string user_id { get => _user_id; set => _user_id = value; }

        public bool IsComplete()
        {
            return _user_id != null;
        }
    }
}
