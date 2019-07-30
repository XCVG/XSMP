using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace XSMP.ApiSurface
{
    public class APISurface : IDisposable
    {

        #region Infrastructure

        private List<APIMapping> Mappings;

        //TODO constructor with pseudo-DI

        public APISurface()
        {
            PrepareMappings();
            Console.WriteLine($"[APISurface] {Mappings.Count} mappings in list");
        }

        /// <summary>
        /// Preloads the list of mappings using reflections
        /// </summary>
        private void PrepareMappings()
        {
            var methods = Assembly.GetExecutingAssembly().GetTypes()
                .Select(t => t.GetMethods()).SelectMany(i => i)
                .Where(t => t.GetCustomAttribute<APIMethodAttribute>() != null);

            Mappings = new List<APIMapping>();

            foreach(var method in methods)
            {
                var attr = method.GetCustomAttribute<APIMethodAttribute>();
                var mapping = new APIMapping(attr.Mapping, attr.Verb, method);
                Mappings.Add(mapping);
            }
        }

        public void Dispose()
        {
            //nop for now
        }

        public async Task<string> Call(HttpListenerRequest request)
        {
            //reject certain things out of hand
            if (request.ContentType == "application/coffee-pot-command")
                throw new TeapotException();

            //TODO split strings, find matching method, and call

            throw new NotImplementedException();
        }
        

        private readonly struct APIMapping
        {
            public readonly string Mapping;
            public readonly HttpVerb Verb;
            public readonly MethodInfo Method;

            public APIMapping(string mapping, HttpVerb verb, MethodInfo method)
            {
                Mapping = mapping;
                Verb = verb;
                Method = method;
            }
        }

        #endregion

        #region API

        [APIMethod(Mapping = "meta/status", Verb = HttpVerb.GET)]
        public string GetSystemStatus(string requestSegment, string requestBody)
        {
            return JsonConvert.SerializeObject(new { status = "WIP", description = "It's not done yet" });
        }

        #endregion

    }


}
