using System;
using System.Collections.Generic;
using System.Net;
using DevCycle.SDK.Server.Common.Model;
using DevCycle.SDK.Server.Common.Model.Cloud;
using Moq;
using Newtonsoft.Json;
using RestSharp;
using RichardSzalay.MockHttp;

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

        public static Variable GetVariableByKeyAsync()
        {
            return new Variable(Guid.NewGuid().ToString(), "test-false", TypeEnum.Boolean, false);
        }

        public static Dictionary<string, Variable> GetVariablesAsync()
        {
            return new Dictionary<string, Variable>
            {
                {
                    "test-false",
                    new Variable(Guid.NewGuid().ToString(), "test-false", TypeEnum.Boolean, false)
                }
            };
        }

        public static DVCResponse GetTrackResponseAsync(int count)
        {
            return new DVCResponse($"Successfully received {count} events");
        }
    }
}
