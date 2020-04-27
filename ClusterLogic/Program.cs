using ClusterLogic.Models;
using ClusterLogic.NLPHandler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterAPI
{
    class Program
    {
        static void Main(string[] args)
        {
            /*
            int command = -1;

            while (int.TryParse(Console.ReadLine(), out command)) {
                if (command == -1)
                {
                    break;
                }

                switch (command)
                {
                    case 1:
                        MatchQuestionModelResponse mqmr = new MatchQuestionModelResponse();
                        mqmr.msg_id = 67;
                        mqmr.possible_matches = new MatchQuestionModelInfo[] {
                            new MatchQuestionModelInfo() { prob = .85f, question_id = 5},
                            new MatchQuestionModelInfo() { prob = .6f, question_id = 7},
                            new MatchQuestionModelInfo() { prob = .15f, question_id = 1}
                        };
                        mqmr.question_id = 23;
                        ProcessNLPResponse.ProcessNLPMatchQuestionsResponse(mqmr);
                        break;
                }
            }
            */
            String s = "This is a \"test\"";
            Console.Out.WriteLine(s);
            String s2 = UserInputToSQLSafe(s);
            Console.Out.WriteLine(s2);
            String s3 = SQLSafeToUserInput(s2);
            Console.Out.WriteLine(s3);
        }

        static Dictionary<char, String> forbiddenSQL = null;
        public static String UserInputToSQLSafe(String userInput)
        {
            if (forbiddenSQL == null)
            {
                CreateForbiddenDict();
            }
            int index = 0;
            while (index < userInput.Length)
            {
                char c = userInput[index];
                if (forbiddenSQL.ContainsKey(c))
                {
                    String value = " ";
                    forbiddenSQL.TryGetValue(c, out value);
                    userInput = userInput.Insert(index, value);
                    index += value.Length;
                    userInput = userInput.Remove(index, 1);
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
            foreach (var item in forbiddenSQL)
            {
                while (userInput.Contains(item.Value))
                {
                    userInput = userInput.Replace(item.Value, item.Key.ToString());
                }
            }
            return userInput;
        }

        private static void CreateForbiddenDict()
        {
            forbiddenSQL = new Dictionary<char, string>();
            forbiddenSQL.Add('\'', "_char1");
            forbiddenSQL.Add('"', "_char2");
        }
    }
}
