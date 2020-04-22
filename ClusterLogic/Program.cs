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

        }
    }
}
