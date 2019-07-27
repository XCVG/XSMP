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
            RESTServer restServer = new RESTServer();

            IsRunning = true;

            while(IsRunning)
            {
                Thread.Sleep(10);
            }

            restServer.Dispose();
        }

        public static void SignalExit()
        {
            IsRunning = false;
        }
    }
}
