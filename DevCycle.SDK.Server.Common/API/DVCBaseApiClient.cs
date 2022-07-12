using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using RestSharp;

namespace DevCycle.SDK.Server.Common.API
{

    public abstract class DVCBaseApiClient : IDVCApiClient
    {
        public abstract void Dispose();
        public abstract string GetServerSDKKey();
        public abstract RestClient GetRestClient();

        public virtual async Task<RestResponse> SendRequestAsync(object json, string urlFragment, Dictionary<string, string> queryParams = null)
        {
            var restClient = GetRestClient();
            var request = new RestRequest(urlFragment, Method.Post);
            var body = JsonConvert.SerializeObject(json);
            request.AddStringBody(body, "application/json");
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("accept", "application/json");
            request.AddHeader("Authorization", GetServerSDKKey());

            if (queryParams == null) return await restClient.ExecuteAsync(request);
            
            foreach (var kvp in queryParams)
                request.AddQueryParameter(kvp.Key, kvp.Value);

            return await restClient.ExecuteAsync(request);
        }
    }
}