            {
﻿using System;
using System.Runtime.InteropServices;
using ClusterClient;
using ClusterClient.Models;

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
                    case "r":
                        RequestQuestions();
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
                var task = con.SendQuestionAsync("abc", "ddfd");
                task.Wait();
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
                var questions = con.RequestUnansweredQuestionsAsync("adfdsf", 10);
                questions.Wait();
                foreach (ServerQuestion question in questions.Result)
                {
                    Console.WriteLine("Question: " + question.question);
                    Console.WriteLine("Question ID: " + question.question_id);
                }
            } 
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
