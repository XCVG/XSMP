using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace XSMP.ApiSurface
{

    /// <summary>
    /// Controller that calls API methods
    /// </summary>
    public class APIController
    {
        private APISurface ApiSurface;
        private List<APIMapping> Mappings;

        public APIController(APISurface api)
        {
            ApiSurface = api;

            PrepareMappings();
            Console.WriteLine($"[APIController] {Mappings.Count} mappings in list");
        }

        /// <summary>
        /// Preloads the list of mappings using reflections
        /// </summary>
        private void PrepareMappings()
        {
            var methods = ApiSurface.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .Where(t => t.GetCustomAttribute<APIMethodAttribute>() != null);

            Mappings = new List<APIMapping>();

            foreach (var method in methods)
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

        public async Task<APIResponse> Call(HttpListenerRequest request)
        {
            //basically the world's shittiest front controller

            //reject certain things out of hand
            if (request.ContentType == "application/coffee-pot-command")
                throw new TeapotException();

            string requestVerb = request.HttpMethod;

            //split strings, find matching method, and call
            string rawUrl = request.RawUrl.Trim();
            if(rawUrl.Contains('?'))
                rawUrl = rawUrl.Substring(0, rawUrl.IndexOf('?'));
            string[] urlSegments = rawUrl.Split('/', StringSplitOptions.RemoveEmptyEntries); //that will probably bite me later

            //discard the first segment
            urlSegments = urlSegments.Skip(1).ToArray();
            int numSegments = urlSegments.Length;

            //match segments to stored methods
            Dictionary<APIMapping, int> matchedMappings = new Dictionary<APIMapping, int>();
            foreach (var potentialMapping in Mappings)
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

                if (matchedSegments > 0)
                {
                    matchedMappings.Add(potentialMapping, matchedSegments);
                }
            }

            APIMapping mapping = default;

            //we can short-circuit two easy cases
            if (matchedMappings.Count == 0)
            {
                throw new NotImplementedException();
            }
            else if (matchedMappings.Count == 1)
            {
                mapping = matchedMappings.First().Key;
            }
            else
            {
                //use the best match
                var sortedMappings = matchedMappings.OrderByDescending(kvp => kvp.Value);
                mapping = sortedMappings.First().Key;
            }

            //get segment and body, parse query string
            //TODO handle POST params?
            string segment = mapping.Segments.Length < numSegments ? string.Join('/', urlSegments.Skip(mapping.Segments.Length)) : string.Empty;
            string body = request.GetBody();
            var parameters = request.Url.ParseQueryString();
            Dictionary<string, string> parametersDict = parameters.AllKeys.ToDictionary(k => k, k => parameters[k]);

            //call method
            APIRequest apiRequest = new APIRequest(request.RawUrl, segment, body, parametersDict);

            if (typeof(Task<APIResponse>).IsAssignableFrom(mapping.Method.ReturnType))
            {
                return await (Task<APIResponse>)mapping.Method.Invoke(ApiSurface, new object[] { apiRequest });
            }
            else
            {
                return (APIResponse)mapping.Method.Invoke(ApiSurface, new object[] { apiRequest });
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

        

    }
}
