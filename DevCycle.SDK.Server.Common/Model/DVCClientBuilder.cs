using System;
using System.Net;
using DevCycle.SDK.Server.Common.API;
using Microsoft.Extensions.Logging;
using RestSharp;

namespace DevCycle.SDK.Server.Common.Model
{

    public abstract class DVCClientBuilder<ClientType, DevCycleOptions, BuilderType> : IClientBuilder<
        ClientType, 
        DevCycleOptions,
        BuilderType
    >
        where ClientType : IDVCClient
        where DevCycleOptions : IDVCOptions
        where BuilderType : DVCClientBuilder<ClientType, DevCycleOptions, BuilderType>
    {
        protected string sdkKey;
        protected DevCycleOptions options;
        protected ILoggerFactory loggerFactory;
        protected EventHandler<DVCEventArgs> initialized;
        protected DVCRestClientOptions restClientOptions;

        protected abstract BuilderType BuilderInstance { get; }

        public BuilderType SetSDKKey(string key)
        {
            sdkKey = key;
            return BuilderInstance;
        }
        
        public BuilderType SetEnvironmentKey(string key)
        {
            sdkKey = key;
            return BuilderInstance;
        }

        public BuilderType SetOptions(DevCycleOptions dvcOptions)
        {
            options = dvcOptions;
            return BuilderInstance;
        }

        public BuilderType SetLogger(ILoggerFactory loggerFactoryProvider)
        {
            loggerFactory = loggerFactoryProvider;
            return BuilderInstance;
        }

        public BuilderType SetRestClientOptions(DVCRestClientOptions options)
        {
            this.restClientOptions = options;
            return BuilderInstance;
        }

        public abstract ClientType Build();
    }
}