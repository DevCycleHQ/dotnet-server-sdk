using System;
using System.Net;
using DevCycle.SDK.Server.Common.API;
using Microsoft.Extensions.Logging;
using RestSharp;

namespace DevCycle.SDK.Server.Common.Model
{

    public abstract class DevCycleClientBuilder<ClientType, DevCycleOptions, BuilderType> : IClientBuilder<
        ClientType, 
        DevCycleOptions,
        BuilderType
    >
        where ClientType : IDevCycleClient
        where DevCycleOptions : IDevCycleOptions
        where BuilderType : DevCycleClientBuilder<ClientType, DevCycleOptions, BuilderType>
    {
        protected string sdkKey;
        protected DevCycleOptions options;
        protected ILoggerFactory loggerFactory;
        protected EventHandler<DevCycleEventArgs> initialized;
        protected DevCycleRestClientOptions restClientOptions;

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

        public BuilderType SetRestClientOptions(DevCycleRestClientOptions options)
        {
            this.restClientOptions = options;
            return BuilderInstance;
        }

        public abstract ClientType Build();
        public abstract DevCycleProvider BuildOpenFeatureProvider();
    }
}