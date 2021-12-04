using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevCycle.Api;
using DevCycle.Model;

namespace Example
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using DVCClient api = new DVCClient("INSERT SDK KEY");
            var user = new User("user_id");

            try
            {
                Dictionary<string, Feature> result = await api.AllFeaturesAsync(user);

                foreach(KeyValuePair<string, Feature> entry in result)
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
