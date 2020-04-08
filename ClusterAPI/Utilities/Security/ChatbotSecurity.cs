using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ClusterAPI.Controllers.Security
{
    public class ChatbotSecurity : ISecurityHandler
    {
        public bool Authenticate(IEnumerable<string> enumerable)
        {
            if (enumerable == null)
            {
                return true;
            }

            return false;
        }
    }
}