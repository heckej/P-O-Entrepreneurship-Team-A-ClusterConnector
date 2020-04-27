using ClusterLogic.Models;
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
        readonly static public String SQLSource = "Data Source=clusterbot.database.windows.net;Initial Catalog=Cluster;User ID=public_access;Password=]JT87v\"4*/}&5BFK;Connect Timeout=30;Encrypt=True;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False;MultipleActiveResultSets=true";
#else
        readonly static public String SQLSource = "Data Source=clusterbot.database.windows.net;Initial Catalog=Cluster;User ID=public_access;Password=]JT87v\"4*/}&5BFK;Connect Timeout=30;Encrypt=True;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False;MultipleActiveResultSets=true";
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

        public static int getAndGenerateMsgIDOpenQuestions(int chatbot_temp_id, string question, string user_id)
        {
            msg_id++;

            msgIdToUserID.Add(msg_id, new NewOpenQuestion(chatbot_temp_id, question, user_id));
            return msg_id;
        }

        public static int getAndGenerateMsgIDForGivenAnswerOffensive(string user_id, string answer, int question_id)
        {
            msg_id++;

            msgIdToUserID.Add(msg_id, new NewAnswerOffenseCheck(question_id, answer, user_id));
            return msg_id;
        }

        static Dictionary<char, String> forbiddenSQL = null;
        public static String UserInputToSQLSafe(String userInput)
        {
            if (forbiddenSQL == null)
            {
                CreateForbiddenDict();
            }
            int index = 0;
            while (index != userInput.Length)
            {
                char c = userInput[index];
                if (forbiddenSQL.ContainsKey(c))
                {
                    userInput = userInput.Insert(index++, "\\");
                }
                index++;
            }
            return userInput;
        }

        public static String SQLSafeToUserInput(String userInput)
        {
            if (forbiddenSQL == null)
            {
                CreateForbiddenDict();
            }
            int index = 1;
            while (index != userInput.Length)
            {
                char c = userInput[index];
                if (forbiddenSQL.ContainsKey(c) && userInput[index - 1] == '\\')
                {
                    userInput = userInput.Remove(index - 1, 1);
                    index = index - 1;
                }
                index++;
            }
            return userInput;
        }

        private static void CreateForbiddenDict()
        {
            forbiddenSQL = new Dictionary<char, string>();
            forbiddenSQL.Add('\'', "\'");
            forbiddenSQL.Add('"', "\\\"");
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

    public struct NewOpenQuestion : ServerData
    {
        public int chatbot_temp_id; public string question; public string user_id;
        public NewOpenQuestion(int chatbot_temp_id, string question, string user_id)
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

        public NewAnswerOffenseCheck(NewAnswerNonsenseCheck newAnswerNonsenseCheck) : this(newAnswerNonsenseCheck.question_id, newAnswerNonsenseCheck.answer,newAnswerNonsenseCheck.user_id)
        {
        }

        public NewAnswerOffenseCheck(int question_id, string answer, string user_id)
        {
            this.question_id = question_id;
            this.answer = answer;
            this.user_id = user_id;
        }
    }
}
