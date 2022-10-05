using System;
using System.Net;
using DevCycle.SDK.Server.Common.API;
using Microsoft.Extensions.Logging;
using RestSharp;

namespace DevCycle.SDK.Server.Common.Model
{
    public interface IClientBuilder
    {
        IClientBuilder SetEnvironmentKey(string key);
        IClientBuilder SetOptions(IDVCOptions options);
        IClientBuilder SetLogger(ILoggerFactory loggerFactoryProvider);
        IClientBuilder SetRestClientOptions(DVCRestClientOptions options);
        IDVCClient Build();
    }
}