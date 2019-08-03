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
            var methods = GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .Where(t => t.GetCustomAttribute<APIMethodAttribute>() != null);

            Mappings = new List<APIMapping>();

            foreach(var method in methods)
            {
                var attr = method.GetCustomAttribute<APIMethodAttribute>();
                var mapping = new APIMapping(attr.Mapping, attr.Verb, method);
                Mappings.Add(mapping);
            }

            //TODO optimization by presort
        }

        public void Dispose()
        {
            //nop for now
        }

        public async Task<string> Call(HttpListenerRequest request)
        {
            //TODO fix wildcards, they're not working

            //basically the world's shittiest front controller

            //reject certain things out of hand
            if (request.ContentType == "application/coffee-pot-command")
                throw new TeapotException();

            string requestVerb = request.HttpMethod;

            //split strings, find matching method, and call
            string[] urlSegments = request.RawUrl.Trim().Split('/', StringSplitOptions.RemoveEmptyEntries); //that will probably bite me later

            //discard the first segment
            urlSegments = urlSegments.Skip(1).ToArray();
            int numSegments = urlSegments.Length;

            //match segments to stored methods
            Dictionary<APIMapping, int> matchedMappings = new Dictionary<APIMapping, int>();
            foreach(var potentialMapping in Mappings)
            {
                //check verbs lol
                if (!string.Equals(potentialMapping.Verb.ToString(), requestVerb, StringComparison.OrdinalIgnoreCase))
                    continue;

                int matchedSegments = 0;

                if (potentialMapping.Segments.Length < numSegments && potentialMapping.UseWildcard)
                {
                    //potentialMapping has less segments than urlSegments and is wildcard -> must match up to potentialMapping.segments
                    if (Enumerable.SequenceEqual<string>(potentialMapping.Segments, urlSegments.Take(potentialMapping.Segments.Length), StringComparer.OrdinalIgnoreCase))
                        matchedSegments = potentialMapping.Segments.Length;
                    
                }
                else if (potentialMapping.Segments.Length == numSegments && !potentialMapping.UseWildcard)
                {
                    //potentialMapping has same number of segments as urlSegments -> all must match
                    if (Enumerable.SequenceEqual<string>(potentialMapping.Segments, urlSegments, StringComparer.OrdinalIgnoreCase))
                        matchedSegments = potentialMapping.Segments.Length;
                }

                if(matchedSegments > 0)
                {
                    matchedMappings.Add(potentialMapping, matchedSegments);
                }
            }

            APIMapping mapping = default;

            //we can short-circuit two easy cases
            if(matchedMappings.Count == 0)
            {
                throw new NotImplementedException();
            }
            else if(matchedMappings.Count == 1)
            {
                mapping = matchedMappings.First().Key;                
            }
            else
            {
                //use the best match
                var sortedMappings = matchedMappings.OrderByDescending(kvp => kvp.Value);
                mapping = sortedMappings.First().Key;
            }

            //get segment and body
            string segment = mapping.Segments.Length < numSegments ? string.Join('/', urlSegments.Skip(mapping.Segments.Length)) : string.Empty;
            string body = request.GetBody();

            //call method
            APIRequest apiRequest = new APIRequest(request.RawUrl, segment, body);

            if (typeof(Task<string>).IsAssignableFrom(mapping.Method.ReturnType))
            {
                return await (Task<string>)mapping.Method.Invoke(this, new object[] { apiRequest });
            }
            else
            {
                return (string)mapping.Method.Invoke(this, new object[] { apiRequest });
            }


            throw new NotImplementedException();
        }
        
        private readonly struct APIMapping
        {
            public readonly string Mapping;
            public readonly HttpVerb Verb;
            public readonly MethodInfo Method;

            public readonly string[] Segments;
            public readonly bool UseWildcard;

            public APIMapping(string mapping, HttpVerb verb, MethodInfo method)
            {
                Mapping = mapping;
                Verb = verb;
                Method = method;

                Segments = mapping.Split('/', StringSplitOptions.RemoveEmptyEntries);
                UseWildcard = mapping.EndsWith('/');
            }
        }

        private readonly struct APIRequest
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

        #endregion

        #region API

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

        #endregion

    }


}
