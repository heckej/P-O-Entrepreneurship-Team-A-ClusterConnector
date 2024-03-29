using ClusterAPI.Utilities.Sockets;
using ClusterConnector.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterConnector
{
    class Program
    {
        static void Main(string[] args)
        {
            // The code provided will print ‘Hello World’ to the console.
            // Press Ctrl+F5 (or go to Debug > Start Without Debugging) to run your app.
            Console.WriteLine("Hello World!");
            //Console.ReadKey();

            ClusterServerSocket clusterServerSocket = new ClusterServerSocket();

            List<int> numbers = new List<int>();
            numbers.AddRange(new List<int>() { 1,2,3,4,5});

            TestQuestion.TestConnection(5);

            Console.ReadKey();

            // Go to http://aka.ms/dotnet-get-started-console to continue learning how to build a console app! 
        }
    }
}
