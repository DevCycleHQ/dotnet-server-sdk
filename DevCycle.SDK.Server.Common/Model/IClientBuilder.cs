using System;
using DevCycle.SDK.Server.Common.API;
using Microsoft.Extensions.Logging;

namespace DevCycle.SDK.Server.Common.Model;

public interface IClientBuilder
{
    IClientBuilder SetEnvironmentKey(string key);
    IClientBuilder SetOptions(DVCOptions options);
    IClientBuilder SetLogger(ILoggerFactory loggerFactoryProvider);
    IDVCClient Build();
}

