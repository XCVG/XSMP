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

    public readonly struct APIRequest
    {
        public readonly string Url;
        public readonly string Segment;
        public readonly string Body;

        public APIRequest(string url, string segment, string body)
        {
            Url = url;
            Segment = segment;
            Body = body;
        }
    }

    public readonly struct APIResponse
    {
        //TODO
    }
}
