using System;
using DevCycle.SDK.Server.Common.API;
using Microsoft.Extensions.Logging;

namespace DevCycle.SDK.Server.Common.Model;

public abstract class DVCClientBuilder : IClientBuilder
{
    protected string environmentKey;
    protected DVCOptions options;
    protected ILoggerFactory loggerFactory;
    protected EventHandler<DVCEventArgs> initialized;

    public IClientBuilder SetEnvironmentKey(string key)
    {
        environmentKey = key;
        return this;
    }

    public IClientBuilder SetOptions(DVCOptions dvcOptions)
    {
        options = dvcOptions;
        return this;
    }
    
    public IClientBuilder SetLogger(ILoggerFactory loggerFactoryProvider)
    {
        loggerFactory = loggerFactoryProvider;
        return this;
    }

    public abstract IDVCClient Build();
}