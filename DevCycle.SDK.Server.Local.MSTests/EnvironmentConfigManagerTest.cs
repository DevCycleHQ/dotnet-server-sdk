using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Local.Api;
using DevCycle.SDK.Server.Local.ConfigManager;
using DevCycle.SDK.Server.Common.Model;
using DevCycle.SDK.Server.Common.Model.Local;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RestSharp;
using RichardSzalay.MockHttp;

namespace DevCycle.SDK.Server.Local.MSTests
{
    [TestClass]
    public class EnvironmentConfigManagerTest
    {
        private Tuple<EnvironmentConfigManager, MockHttpMessageHandler, MockedRequest> getTestConfigManager(
            bool isError = false, bool isRetryableError = false,
            string config =
                "{\"project\":{\"_id\":\"6216420c2ea68943c8833c09\",\"key\":\"default\",\"a0_organization\":\"org_NszUFyWBFy7cr95J\"},\"environment\":{\"_id\":\"6216420c2ea68943c8833c0b\",\"key\":\"development\"},\"features\":[{\"_id\":\"6216422850294da359385e8b\",\"key\":\"test\",\"type\":\"release\",\"variations\":[{\"variables\":[{\"_var\":\"6216422850294da359385e8d\",\"value\":true}],\"name\":\"Variation On\",\"key\":\"variation-on\",\"_id\":\"6216422850294da359385e8f\"},{\"variables\":[{\"_var\":\"6216422850294da359385e8d\",\"value\":false}],\"name\":\"Variation Off\",\"key\":\"variation-off\",\"_id\":\"6216422850294da359385e90\"}],\"configuration\":{\"_id\":\"621642332ea68943c8833c4a\",\"targets\":[{\"distribution\":[{\"percentage\":0.5,\"_variation\":\"6216422850294da359385e8f\"},{\"percentage\":0.5,\"_variation\":\"6216422850294da359385e90\"}],\"_audience\":{\"_id\":\"621642332ea68943c8833c4b\",\"filters\":{\"operator\":\"and\",\"filters\":[{\"values\":[],\"type\":\"all\",\"filters\":[]}]}},\"_id\":\"621642332ea68943c8833c4d\"}],\"forcedUsers\":{}}}],\"variables\":[{\"_id\":\"6216422850294da359385e8d\",\"key\":\"test\",\"type\":\"Boolean\"}],\"variableHashes\":{\"test\":2447239932}}")
        {
            var mockHttp = new MockHttpMessageHandler();
            var statusCode =
                isError
                    ? isRetryableError 
                        ? HttpStatusCode.InternalServerError : HttpStatusCode.BadRequest
                    : HttpStatusCode.OK;
            MockedRequest req = mockHttp.When("https://config-cdn*")
                .Respond(statusCode,
                    new List<KeyValuePair<string, string>>() {new("test etag", "test etag value")},
                    "application/json",
                    isError ? "" : config);
           

            var environmentKey = $"server-{Guid.NewGuid()}";
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var cfgManager = new EnvironmentConfigManager(environmentKey, new DVCLocalOptions(),
                loggerFactory, new LocalBucketing(), restClientOptions: new RestClientOptions()
                    {ConfigureMessageHandler = _ => mockHttp},
                initializedHandler: isError ? DidNotInitializeSubscriber : DidInitializeSubscriber);

            return new Tuple<EnvironmentConfigManager, MockHttpMessageHandler, MockedRequest>(cfgManager, mockHttp,
                req);
        }


        [TestMethod]
        public async Task PollForConfigTest()
        {
            var configManager = getTestConfigManager();
            await configManager.Item1.InitializeConfigAsync();
            await Task.Delay(2000);
            Assert.AreEqual(3, configManager.Item2.GetMatchCount(configManager.Item3));
        }

        [TestMethod]
        public async Task PollForConfigNonRetryableTest()
        {
            var configManager = getTestConfigManager(true);
            await configManager.Item1.InitializeConfigAsync();
            await Task.Delay(2000);
            Assert.AreEqual(1, configManager.Item2.GetMatchCount(configManager.Item3));
        }


        [TestMethod]
        public async Task PollForConfigRetryableTest()
        {
            var configManager = getTestConfigManager(true, true);
            await configManager.Item1.InitializeConfigAsync();
            await Task.Delay(2000);
            Assert.IsTrue(configManager.Item2.GetMatchCount(configManager.Item3) >= 2);
        }

        [TestMethod]
        public async Task OnSuccessNotificationTest()
        {
            var configManager = getTestConfigManager();
            await configManager.Item1.InitializeConfigAsync();
        }

        [TestMethod]
        public async Task OnErrorNotificationTest()
        {
            var configManager = getTestConfigManager(true);
            await configManager.Item1.InitializeConfigAsync();
        }

        [TestMethod]
        public async Task OnExceptionNotificationTest()
        {
            var configManager = getTestConfigManager(true);

            await configManager.Item1.InitializeConfigAsync();
        }

        [TestMethod]
        public async Task initializeConfigAsync_configIsNotFetched_thenFetchedOnNextCall_NotificationIsSuccessful()
        {
            var configManager = getTestConfigManager();
            await configManager.Item1.InitializeConfigAsync();
            await Task.Delay(2000);

            Assert.AreEqual(2, configManager.Item2.GetMatchCount(configManager.Item3));
        }


        private void DidInitializeSubscriber(object o, DVCEventArgs e)
        {
            Assert.IsTrue(e.Success);
        }

        private void DidNotInitializeSubscriber(object o, DVCEventArgs e)
        {
            Assert.IsFalse(e.Success);
            Assert.IsNotNull(e.Error);
        }
    }
}