using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Local.Api;
using DevCycle.SDK.Server.Local.ConfigManager;
using DevCycle.SDK.Server.Common.API;
using DevCycle.SDK.Server.Common.Model;
using DevCycle.SDK.Server.Common.Model.Local;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
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
                loggerFactory, new WASMLocalBucketing(), restClientOptions: new DevCycleRestClientOptions()
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

        // ------------------------------------------------------------------
        // SseUri option
        // ------------------------------------------------------------------

        [TestMethod]
        public async Task SseUri_OverridesHostnameFromConfig_WhenSet()
        {
            const string configHostname = "https://real-sse.devcycle.com";
            const string configPath = "/sse/v1/connect";
            const string overrideHostname = "http://localhost:9000";

            var config = GetConfigWithSse(configHostname, configPath);
            var mockHttp = BuildMockHttp(config);

            var sdkKey = $"server-{Guid.NewGuid()}";
            var cfgManager = new EnvironmentConfigManager(sdkKey,
                new DevCycleLocalOptions(sseUri: overrideHostname),
                LoggerFactory.Create(b => b.AddConsole()), new WASMLocalBucketing(),
                restClientOptions: new DevCycleRestClientOptions { ConfigureMessageHandler = _ => mockHttp });

            await cfgManager.InitializeConfigAsync();

            var storedUri = GetSseUri(GetSseManager(cfgManager));
            Assert.IsNotNull(storedUri, "SSEManager should have been created");
            StringAssert.StartsWith(storedUri, overrideHostname,
                "SSEManager URI should use the SseUri override as its hostname");
            StringAssert.Contains(storedUri, configPath,
                "SSEManager URI should preserve the path from the config response");

            cfgManager.Dispose();
        }

        [TestMethod]
        public async Task SseUri_UsesConfigHostname_WhenNotSet()
        {
            const string configHostname = "https://real-sse.devcycle.com";
            const string configPath = "/sse/v1/connect";

            var config = GetConfigWithSse(configHostname, configPath);
            var mockHttp = BuildMockHttp(config);

            var sdkKey = $"server-{Guid.NewGuid()}";
            var cfgManager = new EnvironmentConfigManager(sdkKey,
                new DevCycleLocalOptions(),
                LoggerFactory.Create(b => b.AddConsole()), new WASMLocalBucketing(),
                restClientOptions: new DevCycleRestClientOptions { ConfigureMessageHandler = _ => mockHttp });

            await cfgManager.InitializeConfigAsync();

            var storedUri = GetSseUri(GetSseManager(cfgManager));
            Assert.IsNotNull(storedUri, "SSEManager should have been created");
            StringAssert.StartsWith(storedUri, configHostname,
                "SSEManager URI should use the hostname from the config response when SseUri is not set");

            cfgManager.Dispose();
        }

        // ------------------------------------------------------------------
        // Subscriber helpers
        // ------------------------------------------------------------------

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

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static MockHttpMessageHandler BuildMockHttp(string config)
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When("https://config-cdn*")
                .Respond(HttpStatusCode.OK,
                    new List<KeyValuePair<string, string>>
                    {
                        new("Etag", "test-etag"),
                        new("Last-Modified", DateTime.Now.AddHours(-1).ToString(CultureInfo.InvariantCulture))
                    },
                    "application/json", config);
            return mockHttp;
        }

        private static string GetConfigWithSse(
            string hostname = "https://sse.devcycle.com",
            string path = "/sse")
        {
            var json = JObject.Parse(Fixtures.Config());
            json["sse"] = new JObject { ["hostname"] = hostname, ["path"] = path };
            return json.ToString();
        }

        private static object GetSseManager(EnvironmentConfigManager manager) =>
            typeof(EnvironmentConfigManager)
                .GetField("sseManager", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(manager);

        private static string GetSseUri(object sseManager) =>
            sseManager == null ? null :
            typeof(SSEManager)
                .GetProperty("sseUri", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(sseManager) as string;
    }
}