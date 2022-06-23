using System;
using System.Net;
using DevCycle.SDK.Server.Common.API;
using Microsoft.Extensions.Logging;

namespace DevCycle.SDK.Server.Common.Model
{

    public abstract class DVCClientBuilder : IClientBuilder
    {
        protected string environmentKey;
        protected IDVCOptions options;
        protected ILoggerFactory loggerFactory;
        protected EventHandler<DVCEventArgs> initialized;
        protected IWebProxy proxy;
        
        public IClientBuilder SetEnvironmentKey(string key)
        {
            environmentKey = key;
            return this;
        }

        public IClientBuilder SetOptions(IDVCOptions dvcOptions)
        {
            options = dvcOptions;
            return this;
        }

        public IClientBuilder SetLogger(ILoggerFactory loggerFactoryProvider)
        {
            loggerFactory = loggerFactoryProvider;
            return this;
        }

        public IClientBuilder SetWebProxy(IWebProxy proxy)
        {
            this.proxy = proxy;
            return this;
        }

        public abstract IDVCClient Build();
    }
}