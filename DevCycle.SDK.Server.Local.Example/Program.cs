using System;
using System.Collections.Generic;
using System.Data;
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

            var apiBuilder = new DevCycleLocalClientBuilder();
            api = apiBuilder
                .SetOptions(new DevCycleLocalOptions())
                .SetInitializedSubscriber((o, e) =>
                {
                    if (e.Success)
                    {
                        ClientInitialized();
                    }
                    else
                    {
                        Console.WriteLine($"Client did not initialize. Errors: {e.Errors}");
                    }
                })
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

        private static void ClientInitialized()
        {
            foreach (var userId in new List<string> { "a", "b", "c" })
            {
                Console.WriteLine("VariableValue-----------");
                var user = new DevCycleUser(userId);
                Console.WriteLine();
                // Double variable - default value integer
                double variableValueDI = api.VariableValue(user, "double-variable", 10);
                // Double variable - typecast to int, default int
                int variableValueII = api.VariableValue(user, "integer-variable", 10);
                // Autoassign type, default value correct
                var variableValueDD = api.VariableValue(user, "double-variable", 10.0);
                // high prec float, default float
                float variableValueFF = api.VariableValue(user, "high-precision-variable", 69.420f);

                var values = new List<Object>() { variableValueDI, variableValueII, variableValueDD, variableValueFF };
                foreach (var k in values)
                {
                    Console.WriteLine("User is {2} - Value is {0} Type of {1}", k, k.GetType(), userId);
                }
                Console.WriteLine("Variables---------------");

                Variable<int> variableDI = api.Variable(user, "double-variable", 10);
                Console.WriteLine("User is {2} - Value is {0} Type of {1}", variableDI.Value, variableDI.Type, userId);
                // Double variable - typecast to int, default int
                var variableII = api.Variable(user, "integer-variable", 10);
                Console.WriteLine("User is {2} - Value is {0} Type of {1}", variableII.Value, variableII.Type, userId);
                // Autoassign type, default value correct
                var variableDD = api.Variable(user, "double-variable", 10.0);
                Console.WriteLine("User is {2} - Value is {0} Type of {1}", variableDD.Value, variableDD.Type, userId);
                // high prec float, default float
                var variableFF = api.Variable(user, "high-precision-variable", 69.420f);
                Console.WriteLine("User is {2} - Value is {0} Type of {1}", variableFF.Value, variableFF.Type, userId);
            }

            api.Dispose();
        }
    }
}