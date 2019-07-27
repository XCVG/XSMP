using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace XSMP.ApiSurface
{
    public class APISurface
    {
        public static APISurface Instance { get; private set; }

        //TODO constructor with pseudo-DI

        [APIMethod(Mapping = "meta/status", Verb = HttpVerb.GET)]
        public string GetSystemStatus(string requestSegment, string requestBody)
        {
            return JsonConvert.SerializeObject(new { status = "WIP", description = "It's not done yet" });
        }

    }
}
