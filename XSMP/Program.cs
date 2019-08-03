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
            Console.WriteLine("Starting XCVG Systems Media Provider v0.1 \"Anette\"");

            AppDomain.CurrentDomain.ProcessExit += new EventHandler(HandleExternalExit);

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

        //I don't think this works at all, but we tried
        private static void HandleExternalExit(object sender, EventArgs e)
        {
            Console.WriteLine("Exit signal received!");

            IsRunning = false;
        }
    }
}
