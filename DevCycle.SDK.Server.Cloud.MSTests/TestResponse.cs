using System;
using System.Collections.Generic;
using DevCycle.SDK.Server.Common.Model;
using DevCycle.SDK.Server.Common.Model.Cloud;


namespace DevCycle.SDK.Server.Cloud.MSTests
{
    public static class TestResponse
    {
        public static Dictionary<string, Feature> GetFeaturesAsync()
        {
            return new Dictionary<string, Feature>
            {
                {
                    "show-feature-history",
                    new Feature(Guid.NewGuid().ToString(), "show-feature-history", Feature.TypeEnum.Release, "variation1", "reason", "Reason Name")
                }
            };
        }

        public static Variable<bool> GetVariableByKeyAsync(string key)
        {
            return new Variable<bool>(key, false, false);
        }

        public static Dictionary<string, Variable<bool>> GetVariablesAsync()
        {
            return new Dictionary<string, Variable<bool>>
            {
                {
                    "test-false",
                    new Variable<bool>("test-false", false, false)
                }
            };
        }

        public static DevCycleResponse GetTrackResponseAsync(int count)
        {
            return new DevCycleResponse($"Successfully received {count} events");
        }
    }
}
