using System;
using ClusterClient;

namespace ClusterClientConsole
{
    class Program
    {
        public static Connector con = new Connector("843iu233d3m4pxb1", "ws://localhost:39160/api/Chatbot/WS", 10);

        static void Main(string[] args)
        {
            bool exit = false;
            string input;
            while (!exit)
            {
                input = Console.ReadLine();
                switch(input)
                {
                    case "a":
                        AnswerQuestion();
                        break;
                    case "q":
                        AskQuestion();
                        break;
                    case "stop":
                        return;
                }
            }
        }

        static void AnswerQuestion()
        {
            try
            {
                con.AnswerQuestion("abc", 1, "answer");
            }
            catch(Exception e)
            {
                Console.WriteLine("An exception occurred while answering:");
                Console.WriteLine(e);
            }
        }

        static void AskQuestion()
        {
            try
            {
                con.SendQuestionAsync("abc", "ddfd");
            }
            catch (Exception e)
            {
                Console.WriteLine("An exception occurred while asking:");
                Console.WriteLine(e);
            }
        }
    }
}
