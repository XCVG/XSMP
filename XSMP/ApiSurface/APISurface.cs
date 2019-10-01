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
using XSMP.MediaDatabase;

namespace XSMP.ApiSurface
{

    /// <summary>
    /// API methods that will be directly exposed via REST
    /// </summary>
    public class APISurface
    {
        private MediaDB MediaDatabase;

        //WIP constructor with pseudo-DI
        public APISurface(MediaDB mediaDatabase)
        {
            MediaDatabase = mediaDatabase;
        }

        [APIMethod(Mapping = "meta/exit", Verb = HttpVerb.POST)]
        private APIResponse PostExitRequest(APIRequest request)
        {
            Program.SignalExit();
            return new APIResponse(JsonConvert.SerializeObject(new { description = "Shutting down XSMP" }));
        }

        [APIMethod(Mapping = "meta/status", Verb = HttpVerb.GET)]
        private APIResponse GetSystemStatus(APIRequest request)
        {
            //for now we only check the MediaDatabase component
            SystemStatus systemStatus;
            string systemStatusDescription;
            switch (MediaDatabase.Status)
            {
                case MediaDBStatus.Unknown:
                    systemStatus = SystemStatus.NotReady;
                    systemStatusDescription = "Media database is in unknown state";
                    break;
                case MediaDBStatus.Loading:
                    systemStatus = SystemStatus.NotReady;
                    systemStatusDescription = "Media database is loading";
                    break;
                case MediaDBStatus.Scanning:
                    systemStatus = SystemStatus.NotReady;
                    systemStatusDescription = "Media database is scanning";
                    break;
                case MediaDBStatus.Ready:
                    systemStatus = SystemStatus.Ready;
                    systemStatusDescription = "Ready";
                    break;
                case MediaDBStatus.Error:
                    systemStatus = SystemStatus.Error;
                    systemStatusDescription = "Media database is in fault state";
                    break;
                default:
                    throw new NotSupportedException();
            }

            return new APIResponse(JsonConvert.SerializeObject(new { status = systemStatus.ToString(), description = systemStatusDescription }));
        }

        //below are test methods

        [APIMethod(Mapping = "meta/status", Verb = HttpVerb.POST)]
        private APIResponse PostSystemStatus(APIRequest request)
        {
            return new APIResponse(JsonConvert.SerializeObject(new { status = "test", description = "Why would you ever POST status?" }));
        }

        [APIMethod(Mapping = "meta/status/", Verb = HttpVerb.GET)]
        private APIResponse GetAnyStatus(APIRequest request)
        {
            return new APIResponse(JsonConvert.SerializeObject(new { status = "test", description = "Hit \"any status\"", segment = request.Segment }));
        }

        [APIMethod(Mapping = "meta/", Verb = HttpVerb.GET)]
        private APIResponse GetAnyMeta(APIRequest request)
        {
            return new APIResponse(JsonConvert.SerializeObject(new { status = "test", description = "You hit the base meta handler", segment = request.Segment }));
        }

        [APIMethod(Mapping = "meta/async", Verb = HttpVerb.GET)]
        private async Task<APIResponse> GetAsync(APIRequest request)
        {
            await Task.Delay(2000);

            return new APIResponse(JsonConvert.SerializeObject(new { status = "test", description = "Hit \"async\""}));
        }

        [APIMethod(Mapping = "meta/static", Verb = HttpVerb.GET)]
        private static APIResponse GetStatic(APIRequest request)
        {
            return new APIResponse(JsonConvert.SerializeObject(new { status = "test", description = "Hit \"static\"" }));
        }

    }


}
