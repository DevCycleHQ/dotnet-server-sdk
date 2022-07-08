using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Local.Api;
using DevCycle.SDK.Server.Local.ConfigManager;
using DevCycle.SDK.Server.Common.Exception;
using DevCycle.SDK.Server.Common.Model;
using DevCycle.SDK.Server.Common.Model.Local;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualBasic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RestSharp;
using RichardSzalay.MockHttp;

namespace DevCycle.SDK.Server.Local.MSTests
{
    [TestClass]
    public class EnvironmentConfigManagerTest
    {
        private ILoggerFactory loggerFactory;
        private DVCLocalOptions localOptions;
        private LocalBucketing localBucketing;

        [TestInitialize]
        public void BeforeEachTest()
        {
            loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            localOptions = new DVCLocalOptions();
            localBucketing = new LocalBucketing();
        }

        private Tuple<EnvironmentConfigManager, MockHttpMessageHandler> getTestConfigManager(bool isError = false, bool isRetryableError = false,
            string config =
                "{\"project\":{\"_id\":\"6216420c2ea68943c8833c09\",\"key\":\"default\",\"a0_organization\":\"org_NszUFyWBFy7cr95J\"},\"environment\":{\"_id\":\"6216420c2ea68943c8833c0b\",\"key\":\"development\"},\"features\":[{\"_id\":\"6216422850294da359385e8b\",\"key\":\"test\",\"type\":\"release\",\"variations\":[{\"variables\":[{\"_var\":\"6216422850294da359385e8d\",\"value\":true}],\"name\":\"Variation On\",\"key\":\"variation-on\",\"_id\":\"6216422850294da359385e8f\"},{\"variables\":[{\"_var\":\"6216422850294da359385e8d\",\"value\":false}],\"name\":\"Variation Off\",\"key\":\"variation-off\",\"_id\":\"6216422850294da359385e90\"}],\"configuration\":{\"_id\":\"621642332ea68943c8833c4a\",\"targets\":[{\"distribution\":[{\"percentage\":0.5,\"_variation\":\"6216422850294da359385e8f\"},{\"percentage\":0.5,\"_variation\":\"6216422850294da359385e90\"}],\"_audience\":{\"_id\":\"621642332ea68943c8833c4b\",\"filters\":{\"operator\":\"and\",\"filters\":[{\"values\":[],\"type\":\"all\",\"filters\":[]}]}},\"_id\":\"621642332ea68943c8833c4d\"}],\"forcedUsers\":{}}}],\"variables\":[{\"_id\":\"6216422850294da359385e8d\",\"key\":\"test\",\"type\":\"Boolean\"}],\"variableHashes\":{\"test\":2447239932}}")
        {
            var mockHttp = new MockHttpMessageHandler();
            
            if(!isError)
                mockHttp.When("https://config-cdn*")
                    .Respond(HttpStatusCode.OK,
                        new List<KeyValuePair<string, string>>() {new("test etag", "test etag value")},
                        "application/json",
                        config);
            else switch (isRetryableError)
            {
                case true:
                    mockHttp.When("https://config-cdn*")
                        .Respond(HttpStatusCode.InternalServerError,
                            new List<KeyValuePair<string, string>>() {new("test etag", "test etag value")},
                            "application/json",
                            "");
                    break;
                case false:
                    mockHttp.When("https://config-cdn*")
                        .Respond(HttpStatusCode.BadRequest,
                            new List<KeyValuePair<string, string>>() {new("test etag", "test etag value")},
                            "application/json",
                            "");
                    break;
            }
            var environmentKey = $"server-{Guid.NewGuid()}";

            var cfgManager = new EnvironmentConfigManager(environmentKey, new DVCLocalOptions(),
                new NullLoggerFactory(), new LocalBucketing(), restClientOptions: new RestClientOptions()
                    {ConfigureMessageHandler = _ => mockHttp}, initializedHandler: isError ? DidNotInitializeSubscriber : DidInitializeSubscriber);

            return new Tuple<EnvironmentConfigManager, MockHttpMessageHandler>(cfgManager,mockHttp);
        }
        

        [TestMethod]
        public async Task PollForConfigTest()
        {
            var configManager = getTestConfigManager();
            await configManager.Item1.InitializeConfigAsync();
            await Task.Delay(2000);
            
        }

        [TestMethod]
        public async Task PollForConfigNonRetryableTest()
        {
            var configManager = new EnvironmentConfigManager("server-key", new DVCLocalOptions(), loggerFactory,
                localBucketing, DidNotInitializeSubscriber);
            await configManager.InitializeConfigAsync();
            await Task.Delay(2000);
            // expect only one request since it failed in a non-retryable way
            // mockRestClient.Verify(
            //     v => v.ExecuteAsync(It.IsAny<RestRequest>(), It.IsAny<System.Threading.CancellationToken>()),
            //     Times.Exactly(1));
        }


        [TestMethod]
        public async Task PollForConfigRetryableTest()
        {
            var configManager = getTestConfigManager(true, true);
            await configManager.Item1.InitializeConfigAsync();
            await Task.Delay(2000);
            // expect several retry requests
            // mockRestClient.Verify(
            //     v => v.ExecuteAsync(It.IsAny<RestRequest>(), It.IsAny<System.Threading.CancellationToken>()),
            //     Times.Exactly(3));
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
            var mockHttp = new MockHttpMessageHandler();

            var configManager = getTestConfigManager();
            await configManager.Item1.InitializeConfigAsync();
            await Task.Delay(2000);

            //Assert.AreEqual(2, configCallCount);
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