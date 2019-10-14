using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace XSMP.MediaDatabase
{

    /// <summary>
    /// Model class representing a playlist
    /// </summary>
    public class Playlist //TODO visibility?
    {
        [JsonProperty(Required = Required.Always)]
        public string NiceName { get; set; }

        [JsonProperty]
        public string Description { get; set; }

        [JsonProperty(Required = Required.Always)]
        public IList<string> Songs { get; private set; } = new List<string>(); //list of song hashes

        [JsonExtensionData]
        public IDictionary<string, JToken> AdditionalData { get; set; }

    }
}
