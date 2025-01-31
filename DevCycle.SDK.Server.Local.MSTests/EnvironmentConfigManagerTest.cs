﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Local.Api;
using DevCycle.SDK.Server.Local.ConfigManager;
using DevCycle.SDK.Server.Common.API;
using DevCycle.SDK.Server.Common.Model;
using DevCycle.SDK.Server.Common.Model.Local;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RestSharp;
using RichardSzalay.MockHttp;

namespace DevCycle.SDK.Server.Local.MSTests
{
    [TestClass]
    public class EnvironmentConfigManagerTest
    {
        private Tuple<EnvironmentConfigManager, MockHttpMessageHandler, MockedRequest> getTestConfigManager(
            bool isError = false,
            bool isRetryableError = false)
        {
            string config = new string(Fixtures.Config());

            var mockHttp = new MockHttpMessageHandler();
            var statusCode =
                isError
                    ? isRetryableError
                        ? HttpStatusCode.InternalServerError
                        : HttpStatusCode.BadRequest
                    : HttpStatusCode.OK;
            MockedRequest req = mockHttp.When("https://config-cdn*")
                .Respond(statusCode,
                    new List<KeyValuePair<string, string>>()
                    {
                        new("Etag", "test etag value"),
                        new("Last-Modified", DateTime.Now.AddHours(-1).ToString(CultureInfo.InvariantCulture))
                    },
                    "application/json",
                    isError ? "" : config);


            var sdkKey = $"server-{Guid.NewGuid()}";
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var cfgManager = new EnvironmentConfigManager(sdkKey, new DevCycleLocalOptions(),
                loggerFactory, new LocalBucketing(), restClientOptions: new DevCycleRestClientOptions()
                    { ConfigureMessageHandler = _ => mockHttp },
                initializedHandler: isError
                    ? (isRetryableError ? DidInitializeSubscriberFailFirstConfigFetch : DidNotInitializeSubscriber)
                    : DidInitializeSubscriber);

            return new Tuple<EnvironmentConfigManager, MockHttpMessageHandler, MockedRequest>(cfgManager, mockHttp,
                req);
        }


        [TestMethod]
        public async Task PollForConfigTest()
        {
            var configManager = getTestConfigManager();
            await configManager.Item1.InitializeConfigAsync();
            await Task.Delay(2000);
            Assert.IsTrue(configManager.Item2.GetMatchCount(configManager.Item3) >= 2);
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

        private void DidInitializeSubscriber(object o, DevCycleEventArgs e)
        {
            Assert.IsTrue(e.Success);
            Assert.AreEqual(0, e.Errors.Count);
        }
        
        private void DidInitializeSubscriberFailFirstConfigFetch(object o, DevCycleEventArgs e)
        {
            Assert.IsFalse(e.Success);
            Assert.AreEqual(0, e.Errors.Count);
        }

        private void DidNotInitializeSubscriber(object o, DevCycleEventArgs e)
        {
            Assert.IsFalse(e.Success);
            Assert.AreNotEqual(0, e.Errors.Count);
        }
    }
}