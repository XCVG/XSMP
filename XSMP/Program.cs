using System;
using System.Threading;
using XSMP.ApiSurface;
using XSMP.RestServer;

namespace XSMP
{
    class Program
    {
        public static bool IsRunning { get; private set; }

        static void Main(string[] args)
        {
            Console.WriteLine("Starting XCVG Systems Media Provider v0.1 \"Alissa\"");

            APISurface apiSurface = new APISurface();
            RESTServer restServer = new RESTServer(apiSurface);
            Console.WriteLine("REST server started!");

            IsRunning = true;

            while(IsRunning)
            {
                Thread.Sleep(10);
                //TODO poll components?
            }

            Console.WriteLine("Ending XSMP");

            restServer.Dispose();
            apiSurface.Dispose();
        }

        public static void SignalExit()
        {
            IsRunning = false;
        }
    }
}
