using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClusterClient;

namespace ClusterClientConsole
{
    class Program
    {
        public static void Main(string[] args)
        {
            var task = Run();
            task.Wait();
        }

        public static async Task Run()
        {
            Connector con = new Connector("ws://localhost:39160/api/Chatbot/WS");
            var start =  con.StartWebSocketConnection();
            var input = GetInput(con);
            var allTasks = new List<Task> { start, input };
            await Task.WhenAny(allTasks);
        }

        public static async Task GetInput(Connector con)
        {
            while (true)
            {
                Console.Write("Enter command (ask/answer/feedback/exit): ");
                string input = Console.ReadLine();
                Console.WriteLine(">> " + input);
                switch (input)
                {
                    case "ask":
                        await SendQuestion(con);
                        break;
                    case "answer":
                        await AnswerQuestion(con);
                        break;
                    case "feedback":
                        await SendFeedback(con);
                        break;
                    case "exit":
                        con.CloseWebSocketConnection();
                        return;
                    default:
                        break;
                }
            }
        }

        public static async Task SendQuestion(Connector con)
        {
            try
            {
                Console.WriteLine("Waiting for send question.");
                var task = con.SendQuestion(1, "Is this a question?", 0.5);
                await task;
                Console.WriteLine("Send question result: " + task);
            }
            catch(TimeoutException e)
            {
                Console.Write("Expected.");
                Console.WriteLine(e);
            }
            catch(Exception e)
            {
                Console.WriteLine("Not expected.");
                Console.WriteLine(e);
            }
        }

        public static async Task AnswerQuestion(Connector con)
        {
            Console.WriteLine("Waiting for answer question.");
            var task = con.AnswerQuestion(1, 1, "Yes, that is a question.");
            await task;
        }

        public static async Task SendFeedback(Connector con)
        {
            Console.WriteLine("Waiting for feedback.");
            var task = con.SendFeedbackOnAnswer(1, 1, 1, 1);
            await task;
        }
    }
}
