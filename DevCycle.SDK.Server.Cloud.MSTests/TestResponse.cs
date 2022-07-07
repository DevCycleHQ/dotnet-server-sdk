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

        public static RestResponse GetVariableByKeyAsync()
        {
            var variable = new Variable(Guid.NewGuid().ToString(), "test-false", TypeEnum.Boolean, false);

            return GenerateResponse(variable);
        }

        public static RestResponse GetVariablesAsync()
        {
            var features = new Dictionary<string, Variable>
            {
                {
                    "test-false",
                    new Variable(Guid.NewGuid().ToString(), "test-false", TypeEnum.Boolean, false)
                }
            };

            return GenerateResponse(features);
        }

        public static RestResponse GetTrackResponseAsync(int count)
        {
            var response = new DVCResponse($"Successfully received {count} events");

            return GenerateResponse(response);
        }

        private static RestResponse GenerateResponse<T>(T objectToSerialize)
        {
            return null;
        }
    }
}
