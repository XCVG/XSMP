using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace XSMP.RestServer
{
    /// <summary>
    /// Error struct to be returned to the client
    /// </summary>
    internal struct Error
    {
        [JsonProperty(PropertyName = "status")]
        public readonly int Status;
        [JsonProperty(PropertyName = "source")]
        public readonly string Source;
        [JsonProperty(PropertyName = "title")]
        public readonly string Title;
        [JsonProperty(PropertyName = "detail")]
        public readonly string Detail;

        public Error(int status, string source, string title, string detail)
        {
            Status = status;
            Source = source;
            Title = title;
            Detail = detail;
        }
    }

    internal class TeapotException : Exception
    {

    }
}
