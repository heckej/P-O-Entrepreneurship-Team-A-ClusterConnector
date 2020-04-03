using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ClusterAPI.Controllers.Security
{
    interface ISecurityHandler
    {
        bool Authenticate(IEnumerable<string> enumerable);
    }
}