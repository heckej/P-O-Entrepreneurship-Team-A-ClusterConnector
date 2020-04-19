using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterConnector
{
    public static class ServerUtilities
    {
        readonly static public String DATE_TIME_FORMAT = "dd/mm/yyyy HH:mm:ss";
#if DEBUG
        readonly static public String SQLSource = "Data Source=clusterbot.database.windows.net;Initial Catalog=Cluster_Copy;User ID=public_access;Password=]JT87v\"4*/}&5BFK;Connect Timeout=30;Encrypt=True;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
#else
        readonly static public String SQLSource = "Data Source=clusterbot.database.windows.net;Initial Catalog=Cluster;User ID=public_access;Password=]JT87v\"4*/}&5BFK;Connect Timeout=30;Encrypt=True;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
#endif
    }
}
