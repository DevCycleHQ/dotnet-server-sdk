using System;
using System.Threading.Tasks;
using RestSharp.Portable;
using RestSharp.Portable.HttpClient;

namespace DevCycle.Api
{
    class DVCApiClient : IDisposable
    {
        private static readonly string BASE_URL = "https://bucketing-api.devcycle.com/";

        private readonly string serverKey;

        private readonly RestClient restClient = new RestClient(BASE_URL);

        private bool disposedValue;

        public DVCApiClient()
        {

        }

        public DVCApiClient(string serverKey)
        {
            this.serverKey = serverKey;
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
            if (!disposedValue)
            {
                if (disposing)
                {
                    restClient.Dispose();
                }

                disposedValue = true;
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
