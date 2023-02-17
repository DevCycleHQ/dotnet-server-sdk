using System;
using System.Net;
using DevCycle.SDK.Server.Common.API;
using Microsoft.Extensions.Logging;
using RestSharp;

namespace DevCycle.SDK.Server.Common.Model
{
    public interface IClientBuilder<ClientType, OptionsType, BuilderType>
        where ClientType : IDVCClient
        where OptionsType: IDVCOptions
        where BuilderType: IClientBuilder<ClientType, OptionsType, BuilderType>
    {
        BuilderType SetSDKKey(string key);
        
        [Obsolete("SetEnvironmentKey is deprecated, please use SetSDKKey() instead.")]
        BuilderType SetEnvironmentKey(string key);
        
        BuilderType SetOptions(OptionsType options);
        
        BuilderType SetLogger(ILoggerFactory loggerFactoryProvider);
        
        BuilderType SetRestClientOptions(DVCRestClientOptions options);

        ClientType Build();
    }
}