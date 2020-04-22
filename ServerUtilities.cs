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
        //readonly static public String SQLSource = "Data Source=clusterbot.database.windows.net;Initial Catalog=Cluster_Copy;User ID=public_access;Password=]JT87v\"4*/}&5BFK;Connect Timeout=30;Encrypt=True;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
        readonly static public String SQLSource = "Data Source=clusterbot.database.windows.net;Initial Catalog=Cluster;User ID=public_access;Password=]JT87v\"4*/}&5BFK;Connect Timeout=30;Encrypt=True;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
#else
        readonly static public String SQLSource = "Data Source=clusterbot.database.windows.net;Initial Catalog=Cluster;User ID=public_access;Password=]JT87v\"4*/}&5BFK;Connect Timeout=30;Encrypt=True;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
#endif

        private static int msg_id = 0;

        public static int getAndGenerateMsgID(int chatbot_temp_id, string question, string user_id)
        {
            msg_id++;

            msgIdToUserID.Add(msg_id, new NewQuestion(chatbot_temp_id,question,user_id));
            return msg_id;
        }

        public static Dictionary<int, ServerData> msgIdToUserID = new Dictionary<int, ServerData>();

        public static int getAndGenerateMsgIDForGivenAnswer(string user_id, string answer, int question_id)
        {
            msg_id++;

            msgIdToUserID.Add(msg_id, new NewAnswerNonsenseCheck(question_id, answer, user_id));
            return msg_id;
        }

        public static int getAndGenerateMsgIDForGivenQuestion(string user_id, string question, int chatbot_temp_id)
        {
            msg_id++;

            msgIdToUserID.Add(msg_id, new NewQuestionNonsenseCheck(chatbot_temp_id, question, user_id));
            return msg_id;
        }
    }

    public interface ServerData
    {

    }

    public struct NewQuestion : ServerData 
    {
        public int chatbot_temp_id;public string question;public string user_id;
        public NewQuestion(int chatbot_temp_id, string question, string user_id)
        {
            this.chatbot_temp_id = chatbot_temp_id;
            this.question = question;
            this.user_id = user_id;
        }
    }

    public struct NewQuestionNonsenseCheck : ServerData
    {
        public int question_id; public string question; public string user_id;
        public NewQuestionNonsenseCheck(int question_id, string question, string user_id)
        {
            this.question_id = question_id;
            this.question = question;
            this.user_id = user_id;
        }
    }

    public struct NewAnswerNonsenseCheck : ServerData
    {
        public int question_id; public string answer; public string user_id;
        public NewAnswerNonsenseCheck(int question_id, string answer, string user_id)
        {
            this.question_id = question_id;
            this.answer = answer;
            this.user_id = user_id;
        }
    }

    public struct NewAnswerOffenseCheck : ServerData
    {
        public int question_id; public string answer; public string user_id;
        public NewAnswerOffenseCheck(int question_id, string answer, string user_id)
        {
            this.question_id = question_id;
            this.answer = answer;
            this.user_id = user_id;
        }
    }
}
