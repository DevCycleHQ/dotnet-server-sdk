using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
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
    public class DevCycleTest
    {
        private DevCycleCloudClient getTestClient(object bodyResponse, DevCycleCloudOptions options = null)
        {
            var mockHttp = new MockHttpMessageHandler();

            mockHttp.When("https://*")
                .Respond(HttpStatusCode.OK, "application/json",
                    JsonConvert.SerializeObject(
                        bodyResponse
                    ));

            DevCycleCloudClient api = new DevCycleCloudClientBuilder()
                .SetRestClientOptions(new DevCycleRestClientOptions() {ConfigureMessageHandler = _ => mockHttp})
                .SetOptions(options ?? new DevCycleCloudOptions())
                .SetSDKKey($"server-{Guid.NewGuid().ToString()}")
                .SetLogger(new NullLoggerFactory())
                .Build();
            return api;
        }

        [TestMethod]
        public async Task GetProductionAllFeatures()
        {
            var sdkKey = Environment.GetEnvironmentVariable("DEVCYCLE_SERVER_SDK_KEY");
            if (string.IsNullOrEmpty(sdkKey))
            {
                Console.WriteLine(
                    "DEVCYCLE_SERVER_SDK_KEY is not set in the environment variables - skipping production features test.");
                return;
            }

            DevCycleCloudClient api = new DevCycleCloudClientBuilder()
                    .SetSDKKey(Environment.GetEnvironmentVariable("DEVCYCLE_SERVER_SDK_KEY"))
                    .SetLogger(new NullLoggerFactory())
                    .Build();
            var resp = await api.AllFeatures(new DevCycleUser("test"));
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

            var result = await api.AllFeatures(user);

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
            const string key = "show-quickstart";
            DevCycleCloudClient api = getTestClient(TestResponse.GetVariableByKeyAsync(key));

            DevCycleUser user = new DevCycleUser("j_test");
            
            var result = await api.Variable(user, key, true);
            AssertUserDefaultsCorrect(user);
            Assert.IsNotNull(result);
            
            // Test the variable properties
            Assert.IsFalse(result.Value);

            var value = await api.VariableValue(user, key, true);
            Assert.IsFalse(value);
        }

        [TestMethod]
        public async Task GetVariablesTest()
        {
            DevCycleCloudClient api = getTestClient(TestResponse.GetVariablesAsync());

            DevCycleUser user = new DevCycleUser("j_test");

            var result = await api.AllVariables(user);

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

            var result = await api.Track(user, userEvent);

            AssertUserDefaultsCorrect(userAndEvents.User);

            Assert.IsNotNull(result);

            Assert.AreEqual("Successfully received 1 events", result.Message);
        }

        [TestMethod]
        public async Task EdgeDBTest()
        {
            DevCycleCloudOptions options = new DevCycleCloudOptions(true);
            using DevCycleCloudClient api = getTestClient(TestResponse.GetTrackResponseAsync(1), options);

            DateTimeOffset now = DateTimeOffset.UtcNow;

            DevCycleUser user = new DevCycleUser("j_test");
            List<DevCycleEvent> events = new List<DevCycleEvent>();
            DevCycleEvent userEvent = new DevCycleEvent("test event", "test target", now.DateTime, 600);
            events.Add(userEvent);
            UserAndEvents userAndEvents = new UserAndEvents(events, user);

            var result = await api.Track(user, userEvent);
        }

        [TestMethod]
        public void Variable_NullUser_ThrowsException()
        {
            using DevCycleCloudClient api = new DevCycleCloudClient("dvc_server_" + Guid.NewGuid().ToString(), new NullLoggerFactory());

            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            {
                await api.Variable(null, "some_key", true);
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

        [TestMethod]
        public async Task BeforeHookError_ThrowsException()
        {
            const string key = "show-quickstart";
            DevCycleCloudClient api = getTestClient(TestResponse.GetVariableByKeyAsync(key));
            TestEvalHook hook = new TestEvalHook() { ThrowBefore = true };
            api.AddEvalHook(hook);
            
            var result = await api.Variable(new DevCycleUser("test"), key, true); 
            
            Assert.AreEqual(1, hook.BeforeCallCount);
            Assert.AreEqual(0, hook.AfterCallCount);
            Assert.AreEqual(1, hook.ErrorCallCount);
            Assert.AreEqual(1, hook.ErrorCallCount);
            Assert.IsNotNull(result);
            Assert.AreEqual(key, result.Key);
            Assert.AreEqual(true, result.DefaultValue);
            Assert.AreEqual(TypeEnum.Boolean, result.Type);
            Assert.IsFalse(result.IsDefaulted);
        }

        [TestMethod]
        public async Task AfterHookError_ThrowsException()
        {
            const string key = "show-quickstart";
            DevCycleCloudClient api = getTestClient(TestResponse.GetVariableByKeyAsync(key));
            TestEvalHook hook = new TestEvalHook() { ThrowAfter = true };
            api.AddEvalHook(hook);
            
            var result = await api.Variable(new DevCycleUser("test"), key, true); 

            Assert.AreEqual(1, hook.BeforeCallCount);
            Assert.AreEqual(1, hook.AfterCallCount);
            Assert.AreEqual(1, hook.ErrorCallCount);
            Assert.AreEqual(1, hook.FinallyCallCount);
            Assert.IsNotNull(result);
            Assert.AreEqual(key, result.Key);
            Assert.AreEqual(true, result.DefaultValue);
            Assert.AreEqual(TypeEnum.Boolean, result.Type);
            Assert.IsFalse(result.IsDefaulted);
        }

        [TestMethod]
        public async Task ErrorHookError_ThrowsException()
        {
            const string key = "show-quickstart";
            DevCycleCloudClient api = getTestClient(TestResponse.GetVariableByKeyAsync(key));
            TestEvalHook hook = new TestEvalHook() { ThrowError = true, ThrowAfter = true };
            api.AddEvalHook(hook);
            
            var result = await api.Variable(new DevCycleUser("test"), key, true); 

            Assert.AreEqual(1, hook.BeforeCallCount);
            Assert.AreEqual(1, hook.AfterCallCount);
            Assert.AreEqual(1, hook.ErrorCallCount);
            Assert.AreEqual(1, hook.FinallyCallCount);
            Assert.IsNotNull(result);
            Assert.AreEqual(key, result.Key);
            Assert.AreEqual(true, result.DefaultValue);
            Assert.AreEqual(TypeEnum.Boolean, result.Type);
            Assert.IsFalse(result.IsDefaulted);
        }

        [TestMethod]
        public async Task FinallyHookError_ThrowsException()
        {
            const string key = "show-quickstart";
            DevCycleCloudClient api = getTestClient(TestResponse.GetVariableByKeyAsync(key));
            TestEvalHook hook = new TestEvalHook() { ThrowFinally = true };
            api.AddEvalHook(hook);
            
            var result = await api.Variable(new DevCycleUser("test"), "some_key", true); 

            Assert.AreEqual(1, hook.BeforeCallCount);
            Assert.AreEqual(1, hook.AfterCallCount);
            Assert.AreEqual(0, hook.ErrorCallCount);
            Assert.AreEqual(1, hook.FinallyCallCount);
            Assert.IsNotNull(result);
            Assert.AreEqual(key, result.Key);
            Assert.AreEqual(true, result.DefaultValue);
            Assert.AreEqual(TypeEnum.Boolean, result.Type);
            Assert.IsFalse(result.IsDefaulted);
        }

        private void AssertUserDefaultsCorrect(DevCycleUser user)
        {
            Assert.AreEqual("C#", user.Platform);
            Assert.AreEqual(DevCycleUser.SdkTypeEnum.Server, user.SdkType);
            //Assert.AreEqual("1.0.3.0", user.SdkVersion);
        }
    }
}