# DevCycle .NET Server SDK

Welcome to the DevCycle .NET Server SDK, which interfaces with a local bucketing library. This SDK requests config from DevCycle servers on DVCClient initialization. All calls to the client will then perform local bucketing to determine if a user receives a specific variation.
Events are queued and flushed periodically in the background.
This version uses .NET Standard 2.1 and utilizes more resources to perform local bucketing.

## Requirements


### Frameworks supported
- .NET & .NET Core >=3.0
- Mono >=6.4
- Xamarin.iOS >=12.16
- Xamarin.Mac >=5.16
- Xamarin.Android >=10.0
- Unity >=2021.2

### Dependencies
- FubarCoder.RestSharp.Portable.Core >=4.0.8
- FubarCoder.RestSharp.Portable.HttpClient >=4.0.8
- JsonSubTypes >=1.8.0
- Microsoft.Extensions.Logging.Abstractions >= 6.0.1
- Microsoft.Extensions.Logging.Console >= 6.0.1
- Microsoft.Extensions.Logging >= 6.0.0
- Newtonsoft.Json >=13.0.1
- TypeSupport >= 1.1.12
- Wasmtime >= 0.34.0-preview1

## Installation
Download the SDK from Nuget - https://www.nuget.org/packages/DevCycle.DotNet.Server.Local.SDK/1.0.2
and use the namespaces:
```csharp
using DevCycle.Api;
using DevCycle.Client;
using DevCycle.Model;
```
## Getting Started

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevCycle.Api;
using DevCycle.Model;
using Microsoft.Extensions.Logging;

namespace Example
{
    class Program
    {
        private static DVCClient api;
        
        static async Task Main(string[] args)
        {
            var user = new User("test");

            DVCClientBuilder apiBuilder = new DVCClientBuilder();
            api = apiBuilder.SetEnvironmentKey("INSERT_SDK_KEY")
                .SetOptions(new DVCOptions(1000, 5000))
                .SetInitializedSubscriber((o, e) =>
                {
                    if (e.Success)
                    {
                        ClientInitialized(user);
                    }
                    else
                    {
                        Console.WriteLine($"Client did not initialize. Error: {e.Error}");
                    }
                })
                .SetLogger(LoggerFactory.Create(builder => builder.AddConsole()))
                .Build();

            try
            {
                await Task.Delay(5000);
            }
            catch (TaskCanceledException)
            {
                System.Environment.Exit(0);
            }
        }

        private static void ClientInitialized(User user)
        {
            Dictionary<string, Feature> result = api.AllFeatures(user);

            foreach (KeyValuePair<string, Feature> entry in result)
            {
                Console.WriteLine(entry.Key + " : " + entry.Value);
            }
        }
    }
}
```