using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace XSMP.ApiSurface
{
    public class APIMethodAttribute : Attribute
    {
        /// <summary>
        /// The segment of the URL after apin/, terminate with a / to get "all others" with everything past the / passed into the first argument
        /// </summary>
        public string Mapping;
        /// <summary>
        /// The method this applies to
        /// </summary>
        public HttpVerb Verb;
    }

    public enum HttpVerb
    {
        GET, POST, PUT, DELETE
    }
}
