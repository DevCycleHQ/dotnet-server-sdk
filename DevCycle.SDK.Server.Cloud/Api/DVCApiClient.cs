using System;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Common.API;
using RestSharp.Portable;
using RestSharp.Portable.HttpClient;

// ReSharper disable once CheckNamespace
namespace DevCycle.SDK.Server.Cloud.Api
{
    class DVCApiClient : IDVCApiClient
    {
        private static readonly string BASE_URL = "https://bucketing-api.devcycle.com/";

        private readonly string serverKey;

        private readonly RestClient restClient;

        private bool disposed;

        public DVCApiClient()
        {
        }

        public DVCApiClient(string serverKey)
        {
            this.serverKey = serverKey;
            restClient = new RestClient(BASE_URL);
        }

        public string GetServerSDKKey()
        {
            return serverKey;
        }

        public RestClient GetRestClient()
        {
            return restClient;
        }

        public virtual async Task<IRestResponse> SendRequestAsync(Object json, string urlFragment)
        {
            restClient.IgnoreResponseStatusCode = true;
            var request = new RestRequest(urlFragment, Method.POST);
            request.AddJsonBody(json);
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("accept", "application/json");
            request.AddHeader("Authorization", serverKey);

            return await restClient.Execute(request);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    restClient.Dispose();
                }

                disposed = true;
            }
        }

        ~DVCApiClient()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}