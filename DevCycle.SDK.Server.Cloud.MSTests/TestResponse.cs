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

        public static Variable<bool> GetVariableByKeyAsync()
        {
            return new Variable<bool>("test-false", false, false);
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

        public static DVCResponse GetTrackResponseAsync(int count)
        {
            return new DVCResponse($"Successfully received {count} events");
        }
    }
}
