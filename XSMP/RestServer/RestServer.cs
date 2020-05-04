using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using XSMP.ApiSurface;
using XSMP.MediaDatabase;

namespace XSMP.RestServer
{
    public class RESTServer : IDisposable
    {
        private APIController Api;
        private HttpListener Listener;

        public RESTServer(APIController apiController)
        {
            Api = apiController;

            Console.WriteLine($"[RESTServer] Starting listener on {UserConfig.Instance.UrlPrefix}");

            Listener = new HttpListener();
            Listener.Prefixes.Add(UserConfig.Instance.UrlPrefix);
            Listener.Start();

            //I found out there was a native TPL version immediately after writing this
            Listener.BeginGetContext(new AsyncCallback(HandleRequestCallback), Listener);

            Console.WriteLine("[RESTServer] REST server started!");
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
            if(UserConfig.Instance.EnableRequestLogging)
                Console.WriteLine(context.Request.Url);

            var response = context.Response;            

            try
            {
                APIResponse result = await Api.Call(context.Request);
                if (UserConfig.Instance.EnableRequestLogging)
                    Console.WriteLine(result);
                response.ContentType = result.ContentType;
                response.StatusCode = result.StatusCode;
                if (result.RawBody != null)
                {
                    var stream = response.OutputStream;
                    stream.Write(result.RawBody, 0, result.RawBody.Length);
                    stream.Close();
                }
                else
                {
                    response.WriteResponse(result.Body);
                }
            }
            catch (Exception e)
            {
                Exception reportedException;
                if (e is TargetInvocationException)
                    reportedException = e.InnerException;
                else
                    reportedException = e;

                Console.Error.WriteLine($"Error processing request ({e.GetType().Name}: {e.Message})");

                int statusCode = GetStatusCodeForError(reportedException);
                string result = JsonConvert.SerializeObject(
                    new Error(statusCode, "API", reportedException.GetType().Name, reportedException.Message,
                    UserConfig.Instance.EnableStacktrace ? reportedException.StackTrace : string.Empty));
                if (UserConfig.Instance.EnableRequestLogging)
                    Console.WriteLine(result);
                response.ContentType = "application/json";
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
                case MediaDBNotReadyException _:
                    return (int)HttpStatusCode.ServiceUnavailable;
                case ResourceNotFoundException _:
                    return (int)HttpStatusCode.NotFound;
                default:
                    return (int)HttpStatusCode.InternalServerError;
            }
        }

    }
}
