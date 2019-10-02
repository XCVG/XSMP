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

        [APIMethod(Mapping = "meta/status", Verb = HttpVerb.GET)]
        private APIResponse GetSystemStatus(APIRequest request)
        {
            //for now we only check the MediaDatabase component
            SystemStatus systemStatus;
            string systemStatusDescription;
            switch (MediaDatabase.State)
            {
                case MediaDBState.Unknown:
                    systemStatus = SystemStatus.notReady;
                    systemStatusDescription = "Media database is in unknown state";
                    break;
                case MediaDBState.Loading:
                    systemStatus = SystemStatus.notReady;
                    systemStatusDescription = "Media database is loading";
                    break;
                case MediaDBState.Scanning:
                    systemStatus = SystemStatus.notReady;
                    systemStatusDescription = "Media database is scanning";
                    break;
                case MediaDBState.Ready:
                    systemStatus = SystemStatus.ready;
                    systemStatusDescription = "Ready";
                    break;
                case MediaDBState.Error:
                    systemStatus = SystemStatus.error;
                    systemStatusDescription = "Media database is in fault state";
                    break;
                default:
                    throw new NotSupportedException();
            }

            return new APIResponse(JsonConvert.SerializeObject(new { status = systemStatus.ToString(), description = systemStatusDescription }));
        }

        [APIMethod(Mapping = "meta/version", Verb = HttpVerb.GET)]
        private APIResponse GetVersionInfo(APIRequest request)
        {
            return new APIResponse(JsonConvert.SerializeObject(new {
                version = Config.ProductVersion.ToString(),
                apiVersion = Config.APIVersion,
                versionCodename = Config.VersionCodename,
                description = Program.ProductNameString }));
        }

        [APIMethod(Mapping = "meta/exit", Verb = HttpVerb.POST)]
        private APIResponse PostExitRequest(APIRequest request)
        {
            Program.SignalExit();
            return new APIResponse(JsonConvert.SerializeObject(new { description = "Shutting down XSMP" }));
        }

        [APIMethod(Mapping = "meta/refresh", Verb = HttpVerb.POST)]
        private APIResponse PostRefreshDatabase(APIRequest request)
        {
            MediaDatabase.StartMediaScan();
            return new APIResponse(JsonConvert.SerializeObject(new { description = "Starting media scan" }));
        }

        [APIMethod(Mapping = "meta/rebuild", Verb = HttpVerb.POST)]
        private APIResponse PostRebuildDatabase(APIRequest request)
        {
            MediaDatabase.StartRebuild();
            return new APIResponse(JsonConvert.SerializeObject(new { description = "Starting media database rebuild" }));
        }

        [APIMethod(Mapping = "meta/flushcache", Verb = HttpVerb.POST)]
        private APIResponse PostFlushCache(APIRequest request)
        {
            throw new NotImplementedException();
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
