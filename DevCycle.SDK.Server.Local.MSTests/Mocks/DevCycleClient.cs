using System;
using System.Net;
using DevCycle.SDK.Server.Common.API;
using DevCycle.SDK.Server.Common.Model.Local;
using DevCycle.SDK.Server.Local.Api;
using DevCycle.SDK.Server.Local.ConfigManager;
using DevCycle.SDK.Server.Local.MSTests;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RichardSzalay.MockHttp;

public class DevCycleTestClient
{
    public static DevCycleLocalClient getTestClient(DevCycleLocalOptions options = null, string config = null,
            bool skipInitialize = false)
    {
        config ??= new string(Fixtures.Config());

        var mockHttp = new MockHttpMessageHandler();

        mockHttp.When("https://config-cdn*")
            .Respond(skipInitialize ? HttpStatusCode.BadRequest : HttpStatusCode.OK, "application/json",
                config);
        mockHttp.When("https://events*")
            .Respond(HttpStatusCode.Created, "application/json",
                "{}");
        var localBucketing = new LocalBucketing();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var sdkKey = $"dvc_server_{Guid.NewGuid().ToString().Replace('-', '_')}_hash";
        localBucketing.StoreConfig(sdkKey, config);
        var configManager = new EnvironmentConfigManager(sdkKey, options ?? new DevCycleLocalOptions(),
            new NullLoggerFactory(),
            localBucketing,
            restClientOptions: new DevCycleRestClientOptions() { ConfigureMessageHandler = _ => mockHttp });
        configManager.Initialized = !skipInitialize;

        DevCycleLocalClient api = new DevCycleLocalClientBuilder()
            .SetLocalBucketing(localBucketing)
            .SetConfigManager(configManager)
            .SetRestClientOptions(new DevCycleRestClientOptions() { ConfigureMessageHandler = _ => mockHttp })
            .SetOptions(options ?? new DevCycleLocalOptions())
            .SetSDKKey(sdkKey)
            .SetLogger(loggerFactory)
            .Build();
        return api;
    }
}
