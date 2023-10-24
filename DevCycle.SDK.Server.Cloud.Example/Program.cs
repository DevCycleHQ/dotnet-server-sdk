using System;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Cloud.Api;
using DevCycle.SDK.Server.Common.Model;
using Microsoft.Extensions.Logging.Abstractions;

namespace Example
{
    class Program
    {
        private static async Task Main(string[] args)
        {
            var SDK_ENV_VAR = Environment.GetEnvironmentVariable("DEVCYCLE_SERVER_SDK_KEY");

            var api = new DevCycleCloudClientBuilder().SetEnvironmentKey(SDK_ENV_VAR).SetLogger(new NullLoggerFactory()).Build();
            
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
