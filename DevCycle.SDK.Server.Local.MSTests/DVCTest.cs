﻿using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Local.Api;
using DevCycle.SDK.Server.Local.ConfigManager;
using DevCycle.SDK.Server.Common.Model;
using DevCycle.SDK.Server.Common.Model.Cloud;
using DevCycle.SDK.Server.Common.Model.Local;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RichardSzalay.MockHttp;

namespace DevCycle.SDK.Server.Local.MSTests
{
    [TestClass]
    public class DVCTest
    {


        private const string Config =
            "{\"project\":{\"_id\":\"6216420c2ea68943c8833c09\",\"key\":\"default\",\"a0_organization\":\"org_NszUFyWBFy7cr95J\"},\"environment\":{\"_id\":\"6216420c2ea68943c8833c0b\",\"key\":\"development\"},\"features\":[{\"_id\":\"6216422850294da359385e8b\",\"key\":\"test\",\"type\":\"release\",\"variations\":[{\"variables\":[{\"_var\":\"6216422850294da359385e8d\",\"value\":true}],\"name\":\"Variation On\",\"key\":\"variation-on\",\"_id\":\"6216422850294da359385e8f\"},{\"variables\":[{\"_var\":\"6216422850294da359385e8d\",\"value\":false}],\"name\":\"Variation Off\",\"key\":\"variation-off\",\"_id\":\"6216422850294da359385e90\"}],\"configuration\":{\"_id\":\"621642332ea68943c8833c4a\",\"targets\":[{\"distribution\":[{\"percentage\":0.5,\"_variation\":\"6216422850294da359385e8f\"},{\"percentage\":0.5,\"_variation\":\"6216422850294da359385e90\"}],\"_audience\":{\"_id\":\"621642332ea68943c8833c4b\",\"filters\":{\"operator\":\"and\",\"filters\":[{\"values\":[],\"type\":\"all\",\"filters\":[]}]}},\"_id\":\"621642332ea68943c8833c4d\"}],\"forcedUsers\":{}}}],\"variables\":[{\"_id\":\"6216422850294da359385e8d\",\"key\":\"test\",\"type\":\"Boolean\"}],\"variableHashes\":{\"test\":2447239932}}";

        private DVCLocalClient getTestClient(DVCLocalOptions options = null)
        {
            var mockHttp = new MockHttpMessageHandler();

            mockHttp.When("https://config-cdn*")
                .Respond(HttpStatusCode.OK, "application/json",
                    Config);
            mockHttp.When("https://events*")
                .Respond(HttpStatusCode.Created, mediaType: "application/json",
                    "{}");
            var localBucketing = new LocalBucketing();
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var environmentKey = $"server-{Guid.NewGuid()}";
            localBucketing.StoreConfig(environmentKey, Config);
            var configManager = new EnvironmentConfigManager(environmentKey, options ?? new DVCLocalOptions(), new NullLoggerFactory(),
                localBucketing, restClientOptions: new RestClientOptions() {ConfigureMessageHandler = _ => mockHttp});
            configManager.Initialized = true;
            DVCLocalClient api = (DVCLocalClient) new DVCLocalClientBuilder()
                .SetLocalBucketing(localBucketing)
                .SetConfigManager(configManager)
                .SetRestClientOptions(new RestClientOptions() {ConfigureMessageHandler = _ => mockHttp})
                .SetOptions(options ?? new DVCLocalOptions())
                .SetEnvironmentKey(environmentKey)
                .SetLogger(loggerFactory)
                .Build();
            return api;
        }

        [TestMethod]
        public void GetFeaturesTest()
        {
            var api = getTestClient();
            var user = new User("j_test");
            user.Country = "CA";
            user.Language = "en";
            user.AppBuild = 1;
            user.Email = "email@gmail.com";
            user.Name = "name";
            var result = api.AllFeatures(user);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.IsNotNull(result["test"]);
            Assert.IsFalse(string.IsNullOrEmpty(result["test"].VariationKey));
            Assert.IsFalse(string.IsNullOrEmpty(result["test"].VariationName));
        }

        [TestMethod]
        public void GetVariableByKeyTest()
        {
            using DVCLocalClient api = getTestClient();

            var user = new User("j_test");
            string key = "test";
            var result = api.Variable(user, key, false);
            Task.Delay(1000);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Value);
        }

        [TestMethod]
        public void GetJsonVariableByKeyReturnsDefaultTest()
        {
            using DVCLocalClient api = getTestClient();

            var user = new User("j_test");
            string key = "json";

            string json = "['Small','Medium','Large']";
            var expectedValue = JArray.Parse(json);

            var result = api.Variable(user, key, JArray.Parse(json));

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefaulted);
            Assert.AreEqual(expectedValue.ToString(), result.Value.ToString());
        }

        [TestMethod]
        public void GetVariablesTest()
        {
            using DVCLocalClient api = getTestClient();
            Task.Delay(1000);
            User user = new User("j_test");

            var result = api.AllVariables(user);
            // Bucketing needs time to work.
            Task.Delay(1000);
            
            Assert.IsNotNull(result);
            var variable =result.Get<bool>("test");
            Assert.IsNotNull(variable);
            Assert.IsTrue(result.ContainsKey("test"));
            Assert.IsTrue(variable.Value);
        }

        [TestMethod]
        public void PostEventsTest()
        {
            using DVCLocalClient api = getTestClient();

            DateTimeOffset now = DateTimeOffset.UtcNow;
            long unixTimeMilliseconds = now.ToUnixTimeMilliseconds();

            User user = new User("j_test");
            Event userEvent = new Event("test event", "test target", unixTimeMilliseconds, 600);

            api.Track(user, userEvent);
        }

        [TestMethod]
        public void Variable_NullUser_ThrowsException()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                using DVCLocalClient api = getTestClient();

                api.Variable(null, "some_key", true);
            });
        }

        [TestMethod]
        public void User_NullUserId_ThrowsException()
        {
            Assert.ThrowsException<ArgumentException>(() => { _ = new User(); });
        }
    }
}