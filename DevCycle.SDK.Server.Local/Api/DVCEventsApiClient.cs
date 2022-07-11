using System;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Common.API;
using DevCycle.SDK.Server.Common.Model.Local;
using RestSharp;

namespace DevCycle.SDK.Server.Local.Api
{
    internal class DVCEventsApiClient : DVCBaseApiClient
    {
        private const string BaseUrl = "https://events.devcycle.com";
        private const string TrackEventsUrl = "/v1/events/batch";
        private string SdkKey { get; set; }
        private RestClient restClient { get; set; }
        private bool _disposed = false;


        // internal parameterless constructor for testing
        public DVCEventsApiClient()
        {
        }

        public DVCEventsApiClient(string environmentKey, RestClientOptions restClientOptions = null)
        {
            restClientOptions ??= new RestClientOptions()
            {
                BaseUrl = new Uri(BaseUrl)
            };
            if (string.IsNullOrEmpty(restClientOptions.BaseUrl?.ToString()))
                restClientOptions.BaseUrl = new Uri(BaseUrl);
            restClient = new RestClient(restClientOptions);
            SdkKey = environmentKey;
        }

        private void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                restClient.Dispose();
            }

            _disposed = true;
        }

        ~DVCEventsApiClient()
        {
            Dispose(disposing: false);
        }

        public override void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public virtual async Task<RestResponse> PublishEvents(BatchOfUserEventsBatch batch)
        {
            return await SendRequestAsync(batch, TrackEventsUrl);
        }

        public override string GetServerSDKKey()
        {
            return SdkKey;
        }

        public override RestClient GetRestClient()
        {
            return restClient;
        }
    }
}