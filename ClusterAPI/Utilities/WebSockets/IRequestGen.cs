using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterAPI.Utilities.WebSockets
{
    interface IRequestGen
    {
        String GenerateRequest(params Object[] args);
    }
}
