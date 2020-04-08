using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClusterClient;
using ClusterClient.Models;

namespace ClusterClientConsole
{
    class Program
    {
        public static void Main(string[] args)
        {
            Run();
        }

        public static void Run()
        {
            Connector con = new Connector("chatbot", "ws://localhost:39160/api/Chatbot/WS");
            Console.WriteLine("Connector initialized.");
            GetInput(con);
        }

        public static void GetInput(Connector con)
        {
            while (true)
            {
                Console.Write("Enter command (ask/answer/feedback/exit): ");
                string input = Console.ReadLine();
                Console.WriteLine(">> " + input);
                switch (input)
                {
                    case "ask":
                        SendQuestion(con);
                        break;
                    case "answer":
                        AnswerQuestion(con);
                        break;
                    case "feedback":
                        SendFeedback(con);
                        break;
                    case "exit":
                        con.CloseWebSocketConnection();
                        return;
                    default:
                        break;
                }
            }
        }

        public static void SendQuestion(Connector con)
        {
            try
            {
                Console.WriteLine("Waiting for send question.");
                var task = con.SendQuestion(1, "Is this a question?", 0.5);
                task.Wait();
                Console.WriteLine("Send question result: " + task);
            }
            catch (TimeoutException e)
            {
                Console.Write("Expected.");
                Console.WriteLine(e);
            }
            catch (Exception e)
            {
                Console.WriteLine("Not expected.");
                Console.WriteLine(e);
            }
        }

        public static void AnswerQuestion(Connector con)
        {
            Console.WriteLine("Waiting for answer question.");
            con.AnswerQuestion(1, 1, "Yes, that is a question.");
        }

        public static void SendFeedback(Connector con)
        {
            Console.WriteLine("Waiting for feedback.");
            con.SendFeedbackOnAnswer(1, 1, 1, 1);
        }
    }
}
