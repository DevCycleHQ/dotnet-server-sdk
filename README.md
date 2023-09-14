# DevCycle .NET Server SDK

Welcome to the DevCycle .NET Server SDK, which interfaces with a local bucketing library. This SDK requests config from DevCycle servers on DevCycleClient initialization. 
All calls to the client will then perform local bucketing to determine if a user receives a specific variation.
Events are queued and flushed periodically in the background.
This version is compatible with .NET Standard 2.0 and utilizes more resources to perform local bucketing.

## Installation
Download the SDK from Nuget - https://www.nuget.org/packages/DevCycle.SDK.Server.Local/

## Getting Started
Use the example app `DevCycle.SDK.Server.Local.Example`. It will read your DevCycle SDK key from an environment variable `DEVCYCLE_SERVER_SDK_KEY`

Your DevCycle SDK key can be found via [Environments & Keys Settings](https://www.devcycle.com/r/environments) on the DevCycle dashboard.

## Usage
To find usage documentation, visit our docs for [Local Bucketing](https://docs.devcycle.com/docs/sdk/server-side-sdks/dotnet-local).


## Logging

The DevCycle SDK uses [Microsoft.Extensions.Logging](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-2.2) for logging and logs to **stdout** by default. You can customize the logging by providing your own ILoggingFactory when creating the `DevCycleLocalClient`.

```csharp
var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddConsole() // Configure the logger to output to the console
        .SetMinimumLevel(LogLevel.Information); // Set the minimum log level to Information
});

client = new DevCycleLocalClientBuilder()
    .SetSDKKey("<DEVCYCLE_SERVER_SDK_KEY>")
    .SetOptions(options ?? new DevCycleLocalOptions())
    .SetOptions(new DevCycleLocalOptions(configPollingIntervalMs: 60000, eventFlushIntervalMs: 60000))
    .SetLogger(loggerFactory)
    .Build();
```


