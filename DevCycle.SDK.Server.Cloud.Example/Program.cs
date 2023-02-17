using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Cloud.Api;
using DevCycle.SDK.Server.Common.Model;
using DevCycle.SDK.Server.Common.Model.Cloud;
using Microsoft.Extensions.Logging.Abstractions;

namespace Example
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var SDK_ENV_VAR = Environment.GetEnvironmentVariable("DVC_SERVER_SDK_KEY");

            DVCCloudClient api = new DVCCloudClientBuilder().SetEnvironmentKey(SDK_ENV_VAR).SetLogger(new NullLoggerFactory()).Build();
            
            var user = new User("user_id");

            try
            {
                Dictionary<string, Feature> result = await api.AllFeaturesAsync(user);

                foreach(KeyValuePair<string, Feature> entry in result)
                {
                    Console.WriteLine(entry.Key + " : " + entry.Value);
                }

                var variables = await api.AllVariablesAsync(user);
                foreach (KeyValuePair<string, Variable> entry in variables)
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
