using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using RestSharp;
using DevCycle.SDK.Server.Common.Policies;

namespace DevCycle.SDK.Server.Common.API
{

    public abstract class DevCycleBaseApiClient : IDevCycleApiClient
    {
        public abstract void Dispose();
        public abstract string GetServerSDKKey();
        public abstract RestClient GetRestClient();

        public virtual async Task<RestResponse> SendRequestAsync(object json, string urlFragment, Dictionary<string, string> queryParams = null, bool shouldRetry = false)
        {
            var restClient = GetRestClient();
            var request = new RestRequest(urlFragment, Method.Post);
            var body = JsonConvert.SerializeObject(json);
            request.AddStringBody(body, "application/json");
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("accept", "application/json");
            request.AddHeader("Authorization", GetServerSDKKey());

            if (queryParams != null)
            {
                foreach (var kvp in queryParams)
                    request.AddQueryParameter(kvp.Key, kvp.Value);
            }

            if (shouldRetry)
            {
                return await ClientPolicy.GetInstance().ExponentialBackoffRetryPolicyWithTimeout.ExecuteAsync(() => restClient.ExecuteAsync(request));
            }
            return await restClient.ExecuteAsync(request);
        }
    }
}