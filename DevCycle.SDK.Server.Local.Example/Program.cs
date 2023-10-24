using System;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Local.Api;
using DevCycle.SDK.Server.Common.API;
using DevCycle.SDK.Server.Common.Model;
using DevCycle.SDK.Server.Common.Model.Local;
using Microsoft.Extensions.Logging;
using Environment = System.Environment;

namespace Example
{
    class Program
    {
        private static DevCycleLocalClient api;

        public static void Main()
        {
            var SDK_ENV_VAR = Environment.GetEnvironmentVariable("DEVCYCLE_SERVER_SDK_KEY");
            var user = new DevCycleUser("testing");

            var apiBuilder = new DevCycleLocalClientBuilder();

            async void InitializedEventHandler(object o, DevCycleEventArgs e)
            {
                if (e.Success)
                {
                    await ClientInitialized(user);
                }
                else
                {
                    Console.WriteLine($"Client did not initialize. Errors: {e.Errors}");
                }
            }

            api = apiBuilder
                .SetOptions(new DevCycleLocalOptions())
                .SetInitializedSubscriber(InitializedEventHandler)
                .SetRestClientOptions(
                    new DevCycleRestClientOptions()
                    {
                        //...
                    })
                .SetEnvironmentKey(SDK_ENV_VAR)
                .SetLogger(LoggerFactory.Create(builder => builder.AddConsole()))
                .Build();

            api.AddFlushedEventSubscriber((sender, dvcEventArgs) =>
            {
                Console.WriteLine(dvcEventArgs.Errors.Count > 0
                    ? $"Some events were not flushed. Errors: {dvcEventArgs.Errors}"
                    : "Events flushed successfully");
            });


            Task.Delay(15000).Wait();
        }

        private static async Task ClientInitialized(DevCycleUser user)
        {
            var result = await api.AllFeatures(user);

            foreach (var entry in result)
            {
                Console.WriteLine(entry.Key + " : " + entry.Value);
            }

            Console.WriteLine(api.Variable(user, "test-variable", true));
            Console.WriteLine(api.AllVariables(user));
            
            api.Dispose();
        }
    }
}