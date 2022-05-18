using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevCycle.Api;
using DevCycle.SDK.Server.Common.Model;
using Microsoft.Extensions.Logging;
using Environment = System.Environment;

namespace Example
{
    class Program
    {
        private static DVCClient api;
        
        static async Task Main(string[] args)
        {
            
            var SDK_ENV_VAR = Environment.GetEnvironmentVariable("DEVCYCLE_SDK_TOKEN");
            var user = new User("test");

            DVCClientBuilder apiBuilder = new DVCClientBuilder();
            api = (DVCClient) apiBuilder.SetEnvironmentKey(SDK_ENV_VAR)
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