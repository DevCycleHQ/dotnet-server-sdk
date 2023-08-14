using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Common.API;
using DevCycle.SDK.Server.Common.Model.Local;
using RestSharp;

namespace DevCycle.SDK.Server.Local.Api
{
    internal class DevCycleEventsApiClient : DevCycleBaseApiClient
    {
        private const string TrackEventsUrl = "/v1/events/batch";
        private string SdkKey { get; set; }
        private RestClient restClient { get; set; }
        private bool _disposed = false;
        private DevCycleLocalOptions sdkOptions { get; set; }


        // internal parameterless constructor for testing
        public DevCycleEventsApiClient()
        {
        }

        public DevCycleEventsApiClient(string sdkKey, DevCycleLocalOptions options = null, DevCycleRestClientOptions restClientOptions = null)
        {
            options ??= new DevCycleLocalOptions();
            DevCycleRestClientOptions clientOptions = restClientOptions?.Clone() ?? new DevCycleRestClientOptions();
            if (string.IsNullOrEmpty(clientOptions.BaseUrl?.ToString()))
                clientOptions.BaseUrl = new Uri(options.EventsApiUri);
            options.EventsApiCustomHeaders ??= new Dictionary<string, string>();
 
            restClient = new RestClient(clientOptions);
            restClient.AddDefaultHeaders(options.EventsApiCustomHeaders);
            SdkKey = sdkKey;
            sdkOptions = options;
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

        ~DevCycleEventsApiClient()
        {
            Dispose(disposing: false);
        }

        public override void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public virtual async Task<RestResponse> PublishEvents(List<UserEventsBatchRecord> batch)
        {
            var requestBody = new
            {
                batch= batch
            };
            return await SendRequestAsync(requestBody, 
                sdkOptions.EventsApiSlug != "" ? sdkOptions.EventsApiSlug : TrackEventsUrl);
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