using System;
using System.Collections.Generic;
using System.Net;
using DevCycle.Model;
using Moq;
using Newtonsoft.Json;
using RestSharp.Portable;

namespace DevCycle.MSTests
{
    public static class TestResponse
    {
        public static IRestResponse GetFeaturesAsync()
        {
            var features = new Dictionary<string, Feature>
            {
                {
                    "show-feature-history",
                    new Feature(Guid.NewGuid().ToString(), "show-feature-history", Feature.TypeEnum.Release, "variation1", "reason")
                }
            };

            return GenerateResponse(features);
        }

        public static IRestResponse GetVariableByKeyAsync()
        {
            var variable = new Variable(Guid.NewGuid().ToString(), "test-false", Variable.TypeEnum.Boolean, false);

            return GenerateResponse(variable);
        }

        public static IRestResponse GetVariablesAsync()
        {
            var features = new Dictionary<string, Variable>
            {
                {
                    "test-false",
                    new Variable(Guid.NewGuid().ToString(), "test-false", Variable.TypeEnum.Boolean, false)
                }
            };

            return GenerateResponse(features);
        }

        public static IRestResponse GetTrackResponseAsync(int count)
        {
            var response = new DVCResponse(String.Format("Successfully received {0} events", count));

            return GenerateResponse(response);
        }

        private static IRestResponse GenerateResponse<T>(T objectToSerialize)
        {
            var response = new Mock<IRestResponse>();
            response.Setup(_ => _.StatusCode).Returns(HttpStatusCode.OK);
            response.Setup(_ => _.IsSuccess).Returns(true);
            response.Setup(_ => _.Content).Returns(JsonConvert.SerializeObject(objectToSerialize));

            return response.Object;
        }
    }
}
