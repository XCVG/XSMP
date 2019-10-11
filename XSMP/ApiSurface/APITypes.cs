using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net;
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
        public readonly IReadOnlyDictionary<string, string> Params;

        public APIRequest(string url, string segment, string body, IDictionary<string, string> parameters)
        {
            Url = url;
            Segment = segment;
            Body = body;
            Params = (IReadOnlyDictionary<string, string>)parameters?.ToImmutableDictionary() ?? new Dictionary<string, string>();
        }
    }

    public readonly struct APIResponse
    {
        public readonly string Body;
        public readonly int StatusCode;

        public APIResponse(string body) : this(body, (int)HttpStatusCode.OK)
        {

        }

        public APIResponse(string body, int statusCode)
        {
            StatusCode = statusCode;
            Body = body;
        }

        public override string ToString()
        {
            return $"{Body}\n({StatusCode})";
        }
    }
}
