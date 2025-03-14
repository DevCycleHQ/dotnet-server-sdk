﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Local.Api;
using DevCycle.SDK.Server.Common.API;
using DevCycle.SDK.Server.Common.Model;
using DevCycle.SDK.Server.Common.Model.Local;
using Microsoft.Extensions.Logging;
using OpenFeature;
using OpenFeature.Model;
using Environment = System.Environment;

namespace Example
{
    class Program
    {
        private static DevCycleLocalClient api;

        public static async Task Main()
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
            
            
            api.AddFlushedEventSubscriber(async (sender, dvcEventArgs) =>
            {
                Console.WriteLine(dvcEventArgs.Errors.Count > 0
                    ? $"Some events were not flushed. Errors: {dvcEventArgs.Errors}"
                    : "Events flushed successfully");
                await api.AllVariables(user);
            });

            

            Task.Delay(30000).Wait();
        }

        private static async Task ClientInitialized(DevCycleUser user)
        {
            var result = await api.AllFeatures(user);

            foreach (var entry in result)
            {
                Console.WriteLine(entry.Key + " : " + entry.Value);
            }

            Console.WriteLine((await api.Variable(user, "test-variable", true)).ToString());
            Console.WriteLine(await api.AllVariables(user));
            
            
            // Below is an example for OpenFeature. This is not required to 
            EvaluationContext ctx = EvaluationContext.Builder()
                .Set("user_id", "test")
                .Set("customData",
                    new Structure(new Dictionary<string, Value> { { "customkey", new Value("customValue") } }))
                .Set("privateCustomData",
                    new Structure(new Dictionary<string, Value>
                        { { "privateCustomKey", new Value("privateCustomValue") } }))
                .Set("email", "email@email.com")
                .Set("name", "Name Name")
                .Set("language", "EN")
                .Set("country", "CA")
                .Set("appVersion", "0.0.1")
                .Set("appBuild", 1)
                .Set("nonSetValueBubbledCustomData", true)
                .Set("nonSetValueBubbledCustomData2", "true")
                .Set("nonSetValueBubbledCustomData3", 1)
                .Set("nonSetValueBubbledCustomData4", new Value((object)null))
                .Build();
            
            await Api.Instance.SetProviderAsync(api.GetOpenFeatureProvider());
            FeatureClient oFeatureClient = Api.Instance.GetClient();
            var allVariables = await api.AllVariables(DevCycleUser.FromEvaluationContext(ctx));
            foreach (var readOnlyVariable in allVariables)
            {
                switch (readOnlyVariable.Value.Type)
                {
                    case "String":
                        Console.WriteLine(readOnlyVariable.Key + " ---- "+ (await oFeatureClient.GetStringDetailsAsync(readOnlyVariable.Key, "default", ctx)).Reason);
                        break;
                    case "Number":
                        Console.WriteLine(readOnlyVariable.Key + " ---- " + (await oFeatureClient.GetDoubleDetailsAsync(readOnlyVariable.Key, 0d, ctx)).Reason);
                        break;
                    case "JSON":
                        Console.WriteLine(readOnlyVariable.Key + " ---- " + (await oFeatureClient.GetObjectDetailsAsync(readOnlyVariable.Key, null, ctx)).Reason);
                        break;
                    case "Boolean":
                        Console.WriteLine(readOnlyVariable.Key + " ---- " +  (await oFeatureClient.GetBooleanDetailsAsync(readOnlyVariable.Key, false, ctx)).Reason);
                        break;
                }
            }
            // End openfeature example
            api.Dispose();
        }
    }
}