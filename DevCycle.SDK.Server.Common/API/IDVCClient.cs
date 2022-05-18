using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using DevCycle.SDK.Server.Common.Model;
using System.Threading.Tasks;
namespace DevCycle.SDK.Server.Common.API
{
    public interface IClientBuilder
    {
        IClientBuilder SetEnvironmentKey(string key);
        IClientBuilder SetOptions(DVCOptions options);
        IClientBuilder SetInitializedSubscriber(EventHandler<DVCEventArgs> initializedSubscriber);
        IClientBuilder SetLogger(ILoggerFactory loggerFactoryProvider);
        IDVCClient Build();
    }
    public interface IDVCClient : IDisposable
    {

    }
}