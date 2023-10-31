using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Cloud.Api;
using DevCycle.SDK.Server.Common.Model;
using Microsoft.Extensions.Logging.Abstractions;
using OpenFeature;
using OpenFeature.Model;

namespace Example
{
    class Program
    {
        private static async Task Main(string[] args)
        {
            var SDK_ENV_VAR = Environment.GetEnvironmentVariable("DEVCYCLE_SERVER_SDK_KEY");

            var api = new DevCycleCloudClientBuilder().SetEnvironmentKey(SDK_ENV_VAR).SetLogger(new NullLoggerFactory()).Build();
            
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
            
            Api.Instance.SetProvider(api.GetOpenFeatureProvider());
            FeatureClient oFeatureClient = Api.Instance.GetClient();
            var allVariables = await api.AllVariables(DevCycleUser.FromEvaluationContext(ctx));
            foreach (var readOnlyVariable in allVariables)
            {
                switch (readOnlyVariable.Value.Type)
                {
                    case "String":
                        Console.WriteLine(readOnlyVariable.Key + " ---- "+ (await oFeatureClient.GetStringDetails(readOnlyVariable.Key, "default", ctx)).Reason);
                        break;
                    case "Number":
                        Console.WriteLine(readOnlyVariable.Key + " ---- " + (await oFeatureClient.GetDoubleDetails(readOnlyVariable.Key, 0d, ctx)).Reason);
                        break;
                    case "JSON":
                        Console.WriteLine(readOnlyVariable.Key + " ---- " + (await oFeatureClient.GetObjectDetails(readOnlyVariable.Key, null, ctx)).Reason);
                        break;
                    case "Boolean":
                        Console.WriteLine(readOnlyVariable.Key + " ---- " +  (await oFeatureClient.GetBooleanDetails(readOnlyVariable.Key, false, ctx)).Reason);
                        break;
                }
            }
            // End openfeature example
            var user = new DevCycleUser("user_id");

            try
            {
                var result = await api.AllFeatures(user);

                foreach(var entry in result)
                {
                    Console.WriteLine(entry.Key + " : " + entry.Value);
                }

                var variables = await api.AllVariables(user);
                foreach (var entry in variables)
                {
                    Console.WriteLine(entry.Key + " : " + entry.Value);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception when calling DevCycleClient.AllFeaturesAsync: " + e.Message);
            }
        }
    }
}
