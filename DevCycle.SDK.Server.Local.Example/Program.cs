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