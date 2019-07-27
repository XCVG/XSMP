using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;

namespace XSMP.RestServer
{
    public class RESTServer : IDisposable
    {

        private HttpListener Listener;
        private Thread ListenerThread;

        private bool IsListening;

        public RESTServer()
        {
            Listener = new HttpListener();
            Listener.Prefixes.Add(@"http://localhost:1547/");
            Listener.Start();

            IsListening = true;

            ListenerThread = new Thread(HandleRequests);
            ListenerThread.Start();            
        }

        public void Dispose()
        {
            Listener.Stop();

            //stupid but okay for now
            IsListening = false;
            Thread.Sleep(10);
            if(ListenerThread.IsAlive)
                ListenerThread.Abort();
        }

        private void HandleRequests()
        {
            while(IsListening)
            {
                var context = Listener.GetContext();
                Console.WriteLine(context.Request.Url);

                var response = context.Response;
                response.StatusCode = (int)HttpStatusCode.OK;

                WriteResponse(response, "Hello World!");
            }
        }

        private void WriteResponse(HttpListenerResponse response, string content)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(content);
            response.ContentLength64 = buffer.Length;
            var stream = response.OutputStream;
            stream.Write(buffer, 0, buffer.Length);
            stream.Close();
        }

    }
}
