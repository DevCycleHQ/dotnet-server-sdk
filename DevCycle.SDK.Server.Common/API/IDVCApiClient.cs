using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RestSharp;

namespace DevCycle.SDK.Server.Common.API
{
    public interface IDVCApiClient : IDisposable
    {
        public string GetServerSDKKey();
        public RestClient GetRestClient();

        public Task<RestResponse> SendRequestAsync(object json, string urlFragment, Dictionary<string, string> queryParams = null, bool shouldRetry = false);
    }
}