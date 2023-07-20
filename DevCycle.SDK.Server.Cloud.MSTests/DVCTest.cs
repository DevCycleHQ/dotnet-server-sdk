using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Cloud.Api;
using DevCycle.SDK.Server.Common.API;
using DevCycle.SDK.Server.Common.Model;
using DevCycle.SDK.Server.Common.Model.Cloud;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using RichardSzalay.MockHttp;

namespace DevCycle.SDK.Server.Cloud.MSTests
{
    [TestClass]
    public class DVCTest
    {
        private DevCycleCloudClient getTestClient(object bodyResponse, DVCCloudOptions options = null)
        {
            var mockHttp = new MockHttpMessageHandler();

            mockHttp.When("https://*")
                .Respond(HttpStatusCode.OK, "application/json",
                    JsonConvert.SerializeObject(
                        bodyResponse
                    ));

            DevCycleCloudClient api = new DevCycleCloudClientBuilder()
                .SetRestClientOptions(new DVCRestClientOptions() {ConfigureMessageHandler = _ => mockHttp})
                .SetOptions(options ?? new DVCCloudOptions())
                .SetSDKKey($"server-{Guid.NewGuid().ToString()}")
                .SetLogger(new NullLoggerFactory())
                .Build();
            return api;
        }

        [TestMethod]
        public async Task GetProductionAllFeatures()
        {
            var sdkKey = Environment.GetEnvironmentVariable("DVC_SERVER_SDK_KEY");
            if (string.IsNullOrEmpty(sdkKey))
            {
                Console.WriteLine(
                    "DVC_SERVER_SDK_KEY is not set in the environment variables - skipping production features test.");
                return;
            }

            DevCycleCloudClient api = new DevCycleCloudClientBuilder()
                    .SetSDKKey(Environment.GetEnvironmentVariable("DVC_SERVER_SDK_KEY"))
                    .SetLogger(new NullLoggerFactory())
                    .Build();
            var resp = await api.AllFeaturesAsync(new DevCycleUser("test"));
            Assert.IsTrue(resp.Count > 0);
            foreach (var (key, value) in resp)
            {
                Console.WriteLine(key, value);    
            }
        }

        [TestMethod]
        public async Task GetFeaturesTest()
        {
            DevCycleCloudClient api = getTestClient(TestResponse.GetFeaturesAsync());
            DevCycleUser user = new DevCycleUser("j_test");

            var result = await api.AllFeaturesAsync(user);

            AssertUserDefaultsCorrect(user);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.IsNotNull(result["show-feature-history"]);
            Assert.IsFalse(string.IsNullOrEmpty(result["show-feature-history"].VariationKey));
            Assert.IsFalse(string.IsNullOrEmpty(result["show-feature-history"].VariationName));
        }

        [TestMethod]
        public async Task GetVariableByKeyTest()
        {
            DevCycleCloudClient api = getTestClient(TestResponse.GetVariableByKeyAsync());

            DevCycleUser user = new DevCycleUser("j_test");

            const string key = "show-quickstart";
            var result = await api.VariableAsync(user, key, true);
            AssertUserDefaultsCorrect(user);
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Value);

            var value = await api.VariableValueAsync(user, key, true);
            Assert.IsFalse(value);
        }

        [TestMethod]
        public async Task GetVariablesTest()
        {
            DevCycleCloudClient api = getTestClient(TestResponse.GetVariablesAsync());

            DevCycleUser user = new DevCycleUser("j_test");

            var result = await api.AllVariablesAsync(user);

            AssertUserDefaultsCorrect(user);

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task PostEventsTest()
        {
            using DevCycleCloudClient api = getTestClient(TestResponse.GetTrackResponseAsync(1));

            DateTimeOffset now = DateTimeOffset.UtcNow;

            DevCycleUser user = new DevCycleUser("j_test");
            List<DevCycleEvent> events = new List<DevCycleEvent>();
            DevCycleEvent userEvent = new DevCycleEvent("test event", "test target", now.DateTime, 600);
            events.Add(userEvent);
            UserAndEvents userAndEvents = new UserAndEvents(events, user);

            var result = await api.TrackAsync(user, userEvent);

            AssertUserDefaultsCorrect(userAndEvents.User);

            Assert.IsNotNull(result);

            Assert.AreEqual("Successfully received 1 events", result.Message);
        }

        [TestMethod]
        public async Task EdgeDBTest()
        {
            DVCCloudOptions options = new DVCCloudOptions(true);
            using DevCycleCloudClient api = getTestClient(TestResponse.GetTrackResponseAsync(1), options);

            DateTimeOffset now = DateTimeOffset.UtcNow;

            DevCycleUser user = new DevCycleUser("j_test");
            List<DevCycleEvent> events = new List<DevCycleEvent>();
            DevCycleEvent userEvent = new DevCycleEvent("test event", "test target", now.DateTime, 600);
            events.Add(userEvent);
            UserAndEvents userAndEvents = new UserAndEvents(events, user);

            var result = await api.TrackAsync(user, userEvent);
        }

        [TestMethod]
        public void Variable_NullUser_ThrowsException()
        {
            using DevCycleCloudClient api = new DevCycleCloudClient("dvc_server" + Guid.NewGuid().ToString(), new NullLoggerFactory());

            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            {
                await api.VariableAsync(null, "some_key", true);
            });
        }

        [TestMethod]
        public void User_NullUserId_ThrowsException()
        {
            Assert.ThrowsException<ArgumentException>(() =>
            {
                DevCycleUser user = new DevCycleUser();
            });
        }

        private void AssertUserDefaultsCorrect(DevCycleUser user)
        {
            Assert.AreEqual("C# Cloud", user.Platform);
            Assert.AreEqual(DevCycleUser.SdkTypeEnum.Server, user.SdkType);
            //Assert.AreEqual("1.0.3.0", user.SdkVersion);
        }
    }
}