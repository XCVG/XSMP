using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using XSMP.ApiSurface;

namespace XSMP.RestServer
{
    public class RESTServer : IDisposable
    {
        private APISurface Api;
        private HttpListener Listener;

        public RESTServer(APISurface apiSurface)
        {
            Api = apiSurface;

            Listener = new HttpListener();
            Listener.Prefixes.Add(Config.UrlPrefix);
            Listener.Start();

            //I found out there was a native TPL version immediately after writing this
            Listener.BeginGetContext(new AsyncCallback(HandleRequestCallback), Listener);        
        }

        public void Dispose()
        {
            Listener.Stop(); //probably okay
        }

        /// <summary>
        /// Handles an async request and fires off a Task
        /// </summary>
        private void HandleRequestCallback(IAsyncResult asyncResult)
        {
            HttpListener listener = (HttpListener)asyncResult.AsyncState;
            HttpListenerContext context = Listener.EndGetContext(asyncResult);

            Task.Run(() => HandleRequestAsync(context));

            //setup the next one
            listener.BeginGetContext(new AsyncCallback(HandleRequestCallback), listener);
        }


        /// <summary>
        /// Handles a request async
        /// </summary>
        private async Task HandleRequestAsync(HttpListenerContext context)
        {
            Console.WriteLine(context.Request.Url);

            var response = context.Response;
            response.ContentType = "application/json";

            try
            {
                string result = await Api.Call(context.Request);
                Console.WriteLine(result);
                response.StatusCode = (int)HttpStatusCode.OK;
                response.WriteResponse(result);
            }
            catch (Exception e)
            {
                int statusCode = GetStatusCodeForError(e);
                string result = JsonConvert.SerializeObject(new Error(statusCode, "API", e.GetType().Name, e.Message));
                Console.WriteLine(result);
                response.StatusCode = statusCode;
                response.WriteResponse(result);
            }
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

    }
}
