using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ClusterClient;
using ClusterClient.Models;

namespace ClusterClientConsole
{
    class Program
    {
        public static Connector con = new Connector("843iu233d3m4pxb1", "ws://localhost:39160/api/Chatbot/WS", 1);
        
        static void Main(string[] args)
        {
            con.EndPointAddress = "http://localhost:3978/api/ClusterClient";
            //con.EndPointAddress = "https://penobot.azurewebsites.net/api/ClusterClient";
            con.SurpressConnectionErrors();
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
                    case "r":
                        RequestQuestions();
                        break;
                    case "t":
                        TestProactive();
                        break;
                    case "stop":
                        return;
                }
            }
            con.CloseWebSocketConnection();
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
                con.SendQuestionAndWaitForAnswer("abc", "ddfd");
            }
            catch (Exception e)
            {
                Console.WriteLine("An exception occurred while asking:");
                Console.WriteLine(e);
            }
        }

        static void RequestQuestions()
        {
            try
            {
                var questions = con.RequestAndRetrieveUnansweredQuestions("adfdsf", 10);
                foreach (ServerQuestion question in questions)
                {
                    Console.WriteLine("Question: " + question.question + " at " + DateTime.Now.ToString());
                    Console.WriteLine("Question ID: " + question.question_id);
                }
                Console.WriteLine("Questions received: " + questions.Count);
            } 
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }

        static void TestProactive()
        {
            var answer = new ServerAnswer()
            {
                user_id = "UNJN2HTDH:TNPMJ4WV7",
                action = "answers",
                answer = "Hey",
                answer_id = 1,
                certainty = 1.1,
                chatbot_temp_id = 1,
                question = "Hay",
                question_id = 2,
                status_code = 0,
                status_msg = "OK"
            };
            var answer_questions = new List<ServerQuestion>();
            answer_questions.Add(new ServerQuestion());
            var questions = new ServerQuestionsMessage()
            {
                user_id = "",
                action = Actions.Questions,
                answer_questions = answer_questions,
                status_code = 0,
                status_msg = ""
            };
            var json = JsonSerializer.Serialize<ServerQuestionsMessage>(questions);
            var done = con.SendMessageToEndPointAsync(json, Actions.Questions).Result;
            con.StoreMessageFromServerAsync(json).Wait();
            Console.WriteLine(done);
        }
    }
}
