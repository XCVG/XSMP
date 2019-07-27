﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

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

                //TODO asyncish stuff- I think after this point we're safe

                Console.WriteLine(context.Request.Url);

                var response = context.Response;
                response.ContentType = "application/json";

                try
                {
                    if (context.Request.ContentType == "application/coffee-pot-command")
                        throw new TeapotException();

                    string result = CallAPI(context.Request);
                    Console.WriteLine(result);
                    response.StatusCode = (int)HttpStatusCode.OK;
                    WriteResponse(response, result);
                }
                catch(Exception e)
                {
                    int statusCode = GetStatusCodeForError(e);
                    string result = JsonConvert.SerializeObject(new Error(statusCode, "API", e.GetType().Name, e.Message));
                    Console.WriteLine(result);
                    response.StatusCode = statusCode;
                    WriteResponse(response, result);
                }
                
            }
        }

        private string CallAPI(HttpListenerRequest request) //TODO may make this async
        {
            

            //TODO decode url and call
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the corresponding status code for an exception
        /// </summary>
        private int GetStatusCodeForError(Exception e)
        {
            switch(e)
            {
                //TODO implement 403, 404, 410 etc
                case NotImplementedException _:
                    return (int)HttpStatusCode.NotImplemented;
                case TeapotException _:
                    return 418;
                case TimeoutException _:
                    return 524;
                default:
                    return (int)HttpStatusCode.InternalServerError;
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
