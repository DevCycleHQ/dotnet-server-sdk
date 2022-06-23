using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RestSharp.Portable;
using RestSharp.Portable.HttpClient;

namespace DevCycle.SDK.Server.Common.API
{
    public interface IDVCApiClient : IDisposable
    {
        public string GetServerSDKKey();
        public RestClient GetRestClient();

        public Task<IRestResponse> SendRequestAsync(object json, string urlFragment, Dictionary<string, string> queryParams = null);
    }
}