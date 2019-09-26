using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace XSMP.ApiSurface
{

    /// <summary>
    /// API methods that will be directly exposed via REST
    /// </summary>
    public class APISurface
    {

        //TODO constructor with pseudo-DI
        public APISurface()
        {
            
        }

        [APIMethod(Mapping = "meta/exit", Verb = HttpVerb.POST)]
        private string PostExitRequest(APIRequest request)
        {
            Program.SignalExit();
            return JsonConvert.SerializeObject(new { status = "ending", description = "Shutting down XSMP" });
        }

        [APIMethod(Mapping = "meta/status", Verb = HttpVerb.GET)]
        private string GetSystemStatus(APIRequest request)
        {
            return JsonConvert.SerializeObject(new { status = "WIP", description = "It's not done yet" });
        }

        [APIMethod(Mapping = "meta/status", Verb = HttpVerb.POST)]
        private string PostSystemStatus(APIRequest request)
        {
            return JsonConvert.SerializeObject(new { status = "test", description = "Why would you ever POST status?" });
        }

        [APIMethod(Mapping = "meta/status/", Verb = HttpVerb.GET)]
        private string GetAnyStatus(APIRequest request)
        {
            return JsonConvert.SerializeObject(new { status = "test", description = "Hit \"any status\"", segment = request.Segment });
        }

        [APIMethod(Mapping = "meta/", Verb = HttpVerb.GET)]
        private string GetAnyMeta(APIRequest request)
        {
            return JsonConvert.SerializeObject(new { status = "test", description = "You hit the base meta handler", segment = request.Segment });
        }

        [APIMethod(Mapping = "meta/async", Verb = HttpVerb.GET)]
        private async Task<string> GetAsync(APIRequest request)
        {
            await Task.Delay(2000);

            return JsonConvert.SerializeObject(new { status = "test", description = "Hit \"async\""});
        }

        [APIMethod(Mapping = "meta/static", Verb = HttpVerb.GET)]
        private static string GetStatic(APIRequest request)
        {
            return JsonConvert.SerializeObject(new { status = "test", description = "Hit \"static\"" });
        }

    }


}
