using System;
using System.Collections.Generic;
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
            var SDK_ENV_VAR = Environment.GetEnvironmentVariable("DVC_SERVER_SDK_KEY");

            var api = new DVCCloudClientBuilder().SetEnvironmentKey(SDK_ENV_VAR).SetLogger(new NullLoggerFactory()).Build();
            
            var user = new User("user_id");

            try
            {
                var result = await api.AllFeaturesAsync(user);

                foreach(var entry in result)
                {
                    Console.WriteLine(entry.Key + " : " + entry.Value);
                }

                var variables = await api.AllVariablesAsync(user);
                foreach (var entry in variables)
                {
                    Console.WriteLine(entry.Key + " : " + entry.Value);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception when calling DVCClient.AllFeaturesAsync: " + e.Message);
            }
        }
    }
}
