using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace XSMP.ApiSurface
{
    internal static class APIUtils
    {

        /// <summary>
        /// Gets the body of the request as a string
        /// </summary>
        public static string GetBody(this HttpListenerRequest request)
        {
            string body = null;
            using (var sr = new StreamReader(request.InputStream))
            {
                body = sr.ReadToEnd();
            }

            return body;
        }


        /// <summary>
        /// Writes a string as a response to an HttpListenerResponse and closes the stream
        /// </summary>
        internal static void WriteResponse(this HttpListenerResponse response, string content)
        {
            //might be fucked

            byte[] buffer = Encoding.UTF8.GetBytes(content);
            response.ContentLength64 = buffer.Length;
            var stream = response.OutputStream;
            stream.Write(buffer, 0, buffer.Length);
            stream.Close();
        }

        /// <summary>
        /// Splits a list of comma-separated values, handling the cases of one or no values
        /// </summary>
        /// <remarks>Doesn't yet properly handle escaped commas</remarks>
        internal static string[] SplitCSVList(string csvList)
        {
            if (string.IsNullOrEmpty(csvList))
                return new string[] { };
            else if (!csvList.Contains(','))
                return new string[] { csvList };

            return csvList.Split(',');
        }

        //TODO add a decode string method
    }
}
