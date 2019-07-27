using System;
using System.Threading;
using XSMP.RestServer;

namespace XSMP
{
    class Program
    {
        public static bool IsRunning { get; private set; }

        static void Main(string[] args)
        {
            Console.WriteLine("Starting XCVG Systems Media Provider v0.1 \"Alissa\"");

            RESTServer restServer = new RESTServer();
            Console.WriteLine("REST server started!");

            IsRunning = true;

            while(IsRunning)
            {
                Thread.Sleep(10);
            }

            Console.WriteLine("Ending XSMP");

            restServer.Dispose();
        }

        public static void SignalExit()
        {
            IsRunning = false;
        }
    }
}
