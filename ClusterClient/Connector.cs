using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace ClusterClient
{
    class Connector
    {
        private string websocketHostURI;
        private WebsocketThread websocketThread;

        public Connector(string websocketHostURI)
        {
            this.websocketHostURI = websocketHostURI;

        }

        private void InitializeWebsocketThread()
        {
            this.websocketThread = new WebsocketThread();
        }

        private readonly Queue<string> messagesToBeSent = new Queue<string>();

        public string SendQuestion(int userID, string question) => ""; // ForumQuestion() return questionID

        public string GetNewResponses() => "";

        public string GetQuestions(int amount = 1)
        {
            Console.WriteLine(amount);
            return "";
        }

        public string GetQuestionsAddressedToUser(int userID) => "";

        public void AnswerQuestion(int userID, int questionID)
        {
            Console.WriteLine(userID);
            Console.WriteLine(questionID);
        }

        public async Task SendFeedbackOnAnswer(int userID, int answerID, int questionID, int feedback)
        {
            Console.WriteLine(answerID);
            Console.WriteLine(questionID);
        }
    }

    class UserConnector : Connector
    {
    }



}
