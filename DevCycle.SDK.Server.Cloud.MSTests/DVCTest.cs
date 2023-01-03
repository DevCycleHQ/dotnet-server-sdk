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
using RestSharp;
using RichardSzalay.MockHttp;
using TypeSupport.Extensions;

namespace DevCycle.SDK.Server.Cloud.MSTests
{
    [TestClass]
    public class DVCTest
    {
        private DVCCloudClient getTestClient(object bodyResponse, DVCCloudOptions options = null)
        {
            var mockHttp = new MockHttpMessageHandler();

            mockHttp.When("https://*")
                .Respond(HttpStatusCode.OK, "application/json",
                    JsonConvert.SerializeObject(
                        bodyResponse
                    ));

            DVCCloudClient api = new DVCCloudClientBuilder()
                .SetRestClientOptions(new DVCRestClientOptions() {ConfigureMessageHandler = _ => mockHttp})
                .SetOptions(options ?? new DVCCloudOptions())
                .SetEnvironmentKey($"server-{Guid.NewGuid().ToString()}")
                .SetLogger(new NullLoggerFactory())
                .Build();
            return api;
        }

        [TestMethod]
        public async Task GetProductionAllFeatures()
        {
            var sdkKey = Environment.GetEnvironmentVariable("DEVCYCLE_SDK_KEY");
            if (string.IsNullOrEmpty(sdkKey))
            {
                Console.WriteLine(
                    "DEVCYCLE_SDK_KEY is not set in the environment variables - skipping production features test.");
                return;
            }

            DVCCloudClient api = new DVCCloudClientBuilder()
                    .SetEnvironmentKey(Environment.GetEnvironmentVariable("DEVCYCLE_SDK_KEY"))
                    .SetLogger(new NullLoggerFactory())
                    .Build();
            var resp = await api.AllFeaturesAsync(new User("test"));
            Assert.IsTrue(resp.Count > 0);
            foreach (var (key, value) in resp)
            {
                Console.WriteLine(key, value);    
            }
        }

        [TestMethod]
        public async Task GetFeaturesTest()
        {
            DVCCloudClient api = getTestClient(TestResponse.GetFeaturesAsync());
            User user = new User("j_test");

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
            DVCCloudClient api = getTestClient(TestResponse.GetVariableByKeyAsync());

            User user = new User("j_test");

            const string key = "show-quickstart";
            var result = await api.VariableAsync(user, key, true);

            AssertUserDefaultsCorrect(user);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Value);
        }

        [TestMethod]
        public async Task GetVariablesTest()
        {
            DVCCloudClient api = getTestClient(TestResponse.GetVariablesAsync());

            User user = new User("j_test");

            var result = await api.AllVariablesAsync(user);

            AssertUserDefaultsCorrect(user);

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task PostEventsTest()
        {
            using DVCCloudClient api = getTestClient(TestResponse.GetTrackResponseAsync(1));

            DateTimeOffset now = DateTimeOffset.UtcNow;

            User user = new User("j_test");
            List<Event> events = new List<Event>();
            Event userEvent = new Event("test event", "test target", now.DateTime, 600);
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
            using DVCCloudClient api = getTestClient(TestResponse.GetTrackResponseAsync(1), options);

            DateTimeOffset now = DateTimeOffset.UtcNow;

            User user = new User("j_test");
            List<Event> events = new List<Event>();
            Event userEvent = new Event("test event", "test target", now.DateTime, 600);
            events.Add(userEvent);
            UserAndEvents userAndEvents = new UserAndEvents(events, user);

            var result = await api.TrackAsync(user, userEvent);
        }

        [TestMethod]
        public void Variable_NullUser_ThrowsException()
        {
            using DVCCloudClient api = new DVCCloudClient("dvc_server" + Guid.NewGuid().ToString(), new NullLoggerFactory());

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
                User user = new User();
            });
        }

        private void AssertUserDefaultsCorrect(User user)
        {
            Assert.AreEqual("C# Cloud", user.Platform);
            Assert.AreEqual(User.SdkTypeEnum.Server, user.SdkType);
            //Assert.AreEqual("1.0.3.0", user.SdkVersion);
        }
    }
}