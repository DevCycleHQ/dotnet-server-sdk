using System;
using System.Collections.Generic;
using DevCycle.SDK.Server.Common.Model;
using DevCycle.SDK.Server.Common.Model.Cloud;
using Newtonsoft.Json.Linq;


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

        public static Variable<string> GetStringVariableAsync()
        {
            return new Variable<string>("test-string", "test-value", "default-value");
        }

        public static Variable<double> GetNumberVariableAsync()
        {
            return new Variable<double>("test-number", 42.5, 0.0);
        }

        public static Variable<JObject> GetJsonVariableAsync()
        {
            var jsonValue = JObject.Parse("{\"key\": \"value\", \"nested\": {\"prop\": 123}}");
            var jsonDefault = JObject.Parse("{\"default\": true}");
            return new Variable<JObject>("test-json", jsonValue, jsonDefault);
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
