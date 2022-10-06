using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Local.Api;
using DevCycle.SDK.Server.Common.API;
using DevCycle.SDK.Server.Common.Model;
using DevCycle.SDK.Server.Common.Model.Local;
using Microsoft.Extensions.Logging;
using RestSharp;
using Environment = System.Environment;

namespace Example
{
    class Program
    {
        private static DVCLocalClient api;

        static async Task Main()
        {
            var SDK_ENV_VAR = Environment.GetEnvironmentVariable("DEVCYCLE_SDK_TOKEN");
            var user = new User("testing");

            DVCLocalClientBuilder apiBuilder = new DVCLocalClientBuilder();
            api = (DVCLocalClient) apiBuilder
                .SetOptions(new DVCLocalOptions())
                .SetInitializedSubscriber((o, e) =>
                {
                    if (e.Success)
                    {
                        ClientInitialized(user);
                    }
                    else
                    {
                        Console.WriteLine($"Client did not initialize. Errors: {e.Errors}");
                    }
                })
                .SetRestClientOptions(
                    new DVCRestClientOptions()
                    {
                        //...
                    })
                .SetEnvironmentKey(SDK_ENV_VAR)
                .SetLogger(LoggerFactory.Create(builder => builder.AddConsole()))
                .Build();
            
            api.AddFlushedEventSubscriber((sender, dvcEventArgs) =>
            {
                if(dvcEventArgs.Errors.Count > 0)
                {
                    Console.WriteLine($"Some events were not flushed. Errors: {dvcEventArgs.Errors}");
                }
                else
                {
                    Console.WriteLine("Events flushed successfully");
                }
            });

            try
            {
                await Task.Delay(15000);
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
            
            Console.WriteLine(api.Variable(user, "test-variable", true));
            Console.WriteLine(api.AllVariables(user));
        }
    }
}