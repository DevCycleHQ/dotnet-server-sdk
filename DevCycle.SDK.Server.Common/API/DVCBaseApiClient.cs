using System.Threading.Tasks;
using RestSharp.Portable;
using RestSharp.Portable.HttpClient;

namespace DevCycle.SDK.Server.Common.API
{

    public abstract class DVCBaseApiClient : IDVCApiClient
    {
        public abstract void Dispose();
        public abstract string GetServerSDKKey();
        public abstract RestClient GetRestClient();

        public virtual async Task<IRestResponse> SendRequestAsync(object json, string urlFragment)
        {
            var restClient = GetRestClient();
            restClient.IgnoreResponseStatusCode = true;
            var request = new RestRequest(urlFragment, Method.POST);
            request.AddJsonBody(json);
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("accept", "application/json");
            request.AddHeader("Authorization", GetServerSDKKey());

            return await restClient.Execute(request);
        }
    }
}