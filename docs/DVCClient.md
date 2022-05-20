# DevCycle.Api.DVCClient

<a name="allFeatures"></a>
# **AllFeatures**
> Dictionary<string, Feature> AllFeatures (User user)

Get all features by user data

### Example
```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevCycle.Api;
using DevCycle.Model;
using Microsoft.Extensions.Logging;

namespace Example
{
    public class AllFeaturesExample
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

### Parameters

| Name     | Type                | Description | 
|----------|---------------------|-------------|
| **user** | [**User**](User.md) |             | 

### Return type

[**Dictionary<string, Feature>**](Feature.md)

[[Back to top]](#) [[Back to README]](../README.md)

<a name="variable"></a>
# **Variable**
> Variable Variable (User user, string key, T defaultValue)

Get variable by key for user data

### Example
```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevCycle.Api;
using DevCycle.Model;
using Microsoft.Extensions.Logging;

namespace Example
{
    public class VariableByKeyExample
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
            string key = "my-bool-variable";
            bool defaultValue = true;
        
            Variable<bool> boolVariable = api.Variable(user, key, defaultValue);

            Console.WriteLine(boolVariable);
        }
    }
}
```

### Parameters

| Name             | Type                | Description                                                                                   |
|------------------|---------------------|-----------------------------------------------------------------------------------------------|
| **user**         | [**User**](User.md) |
| **key**          | **string**          | Variable key                                                                                  |
| **defaultValue** | **T**               | Default Value used if Variable was not in the bucketed config or DVCClient did not initialize | 

### Return type

[**Variable**](Variable.md)

[[Back to top]](#) [[Back to README]](../README.md)

<a name="allVariables"></a>
# **AllVariables**
> Dictionary<string, Variable> AllVariables (User user)

Get all variables for user data

### Example
```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevCycle.Api;
using DevCycle.Model;
using Microsoft.Extensions.Logging;

namespace Example
{
    public class AllVariablesExample
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
            Dictionary<string, Feature> result = api.AllVariables(user);

            foreach (KeyValuePair<string, Variable> entry in result.GetAll())
            {
                Console.WriteLine(entry.Key + " : " + entry.Value);
            }
        }
    }
}
```

### Parameters

| Name     | Type                | Description | 
|----------|---------------------|-------------|
| **user** | [**User**](User.md) |             |  

### Return type

[**Dictionary<string, Variable>**](Variable.md)

[[Back to top]](#) [[Back to README]](../README.md)

<a name="track"></a>
# **Track**
> DVCResponse Track (User user, Event event)

Queue an event to be sent to DevCycle for a user

### Example
```csharp
using System;
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
            DateTimeOffset now = DateTimeOffset.UtcNow;
            long unixTimeMilliseconds = now.ToUnixTimeMilliseconds();
            
            var @event = new Event("test event", "test target", unixTimeMilliseconds, 600);

            try
            {
                api.Track(user, @event);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception when calling DVCClient.Track: " + e.Message );
            }
        }
    }
}
```

### Parameters

N/A

### Return type

N/A

[[Back to top]](#) [[Back to README]](../README.md)

<a name="flushEvents"></a>
# **FlushEvents**
> FlushEvents ()

Immediately send queued events to the DevCycle servers

### Example
```csharp
using System;
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
            api.FlushedEvents += (sender, args) =>
            {
                FlushedEvents(args);
            };
            api.FlushEvents();
        }

        private static void FlushedEvents(DVCEventArgs args)
        {
            if (!args.Success)
            {
                Console.WriteLine(args.Error);
            }
        }
    }
}
```

### Parameters

N/A

### Return type

N/A

[[Back to top]](#) [[Back to README]](../README.md)
