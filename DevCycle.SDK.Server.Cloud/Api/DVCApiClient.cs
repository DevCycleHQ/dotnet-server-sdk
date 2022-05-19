using System;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Common.API;
using RestSharp.Portable;
using RestSharp.Portable.HttpClient;

// ReSharper disable once CheckNamespace
namespace DevCycle.SDK.Server.Cloud.Api
{
    class DVCApiClient : DVCBaseApiClient
    {
        private static readonly string BaseUrl = "https://bucketing-api.devcycle.com/";

        private readonly string serverKey;

        private readonly RestClient restClient;

        private bool disposed;

        public DVCApiClient()
        {
        }

        public DVCApiClient(string serverKey)
        {
            this.serverKey = serverKey;
            restClient = new RestClient(BaseUrl);
        }

        public override string GetServerSDKKey()
        {
            return serverKey;
        }

        public override RestClient GetRestClient()
        {
            return restClient;
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;
            if (disposing)
            {
                restClient.Dispose();
            }

            disposed = true;
        }

        ~DVCApiClient()
        {
            Dispose(disposing: false);
        }

        public override void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}