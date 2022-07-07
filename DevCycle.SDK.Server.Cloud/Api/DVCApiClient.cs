using System;
using System.Net;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Common.API;
using RestSharp;

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

        public DVCApiClient(string serverKey, RestClientOptions restClientOptions = null)
        {
            this.serverKey = serverKey;

            restClientOptions ??= new RestClientOptions();
            restClient = new RestClient(restClientOptions);
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