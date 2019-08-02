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
    }
}
