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
        public readonly string ContentType;
        public readonly byte[] RawBody;

        public APIResponse(string body) : this(body, (int)HttpStatusCode.OK, "application/json", null)
        {

        }

        public APIResponse(string body, int statusCode) : this(body, statusCode, "application/json", null)
        {

        }

        public APIResponse(string body, int statusCode, string contentType, byte[] rawBody)
        {
            Body = body;
            StatusCode = statusCode;            
            ContentType = contentType;
            RawBody = rawBody;
        }

        public override string ToString()
        {
            return $"{Body}\n({StatusCode})";
        }
    }

    public class ResourceNotFoundException : Exception
    {
        public override string Message => "The resource does not exist";
    }
}
