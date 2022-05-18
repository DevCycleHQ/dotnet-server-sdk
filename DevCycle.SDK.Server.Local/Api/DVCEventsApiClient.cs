using System;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Common.API;
using DevCycle.SDK.Server.Common.Model.Local;
using RestSharp.Portable;
using RestSharp.Portable.HttpClient;

namespace DevCycle.SDK.Server.Local.Api
{
    internal class DVCEventsApiClient : IDVCApiClient
    {
        private const string BaseUrl = "https://events.devcycle.com";
        private const string TrackEventsUrl = "/v1/events/batch";
        private string _sdkKey { get; set; }
        private RestClient _client { get; set; }
        private bool disposed = false;


        // internal parameterless constructor for testing
        public DVCEventsApiClient()
        {
        }

        public DVCEventsApiClient(string environmentKey)
        {
            _client = new RestClient(BaseUrl);
            _sdkKey = environmentKey;
        }

        private void Dispose(bool disposing)
        {
            if (disposed) return;
            if (disposing)
            {
                _client.Dispose();
            }

            disposed = true;
        }

        ~DVCEventsApiClient()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public virtual async Task<IRestResponse> SendRequestAsync(object json)
        {
            _client.IgnoreResponseStatusCode = true;
            var request = new RestRequest(TrackEventsUrl, Method.POST);
            request.AddJsonBody(json);
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("accept", "application/json");
            request.AddHeader("Authorization", _sdkKey);

            return await _client.Execute(request);
        }
        
        public virtual async Task<IRestResponse> PublishEvents(BatchOfUserEventsBatch batch)
        {
            return await SendRequestAsync(batch);
        }

        public string GetServerSDKKey()
        {
            return _sdkKey;
        }

        public RestClient GetRestClient()
        {
            return _client;
        }
    }
}