using System;
using DevCycle.SDK.Server.Local.ConfigManager;
using DevCycle.SDK.Server.Local.Api;
using DevCycle.SDK.Server.Common.Model;
using DevCycle.SDK.Server.Common.Model.Local;
using LaunchDarkly.EventSource;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Logging;

namespace DevCycle.SDK.Server.Local.MSTests
{
    internal class TestSSEManager : SSEManager
    {
        public int RestartCalls { get; private set; }
        public TestSSEManager(string uri, EventHandler<StateChangedEventArgs> stateHandler,
            EventHandler<MessageReceivedEventArgs> messageHandler,
            EventHandler<ExceptionEventArgs> errorHandler) : base(uri, stateHandler, messageHandler, errorHandler) {}
        public override void RestartSSE(string uri = null, bool resetBackoffDelay = true)
        {
            RestartCalls++;
            // Do not call base to avoid creating real EventSource objects repeatedly.
        }
        public override void StartSSE() { /* suppress real start */ }
    }

    [TestClass]
    public class SseErrorTests
    {
        private EnvironmentConfigManager BuildManager(int baseIntervalMs = 5000)
        {
            var options = new DevCycleLocalOptions
            {
                ConfigPollingIntervalMs = baseIntervalMs,
                CdnUri = "https://localhost/", // unused in tests; fetch not invoked
                CdnSlug = "/config/test.json",
                DisableRealtimeUpdates = false
            };
            var loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Trace));
            var bucketing = new LocalBucketing();
            var manager = new EnvironmentConfigManager("sdk-test", options, loggerFactory, bucketing);
            // Inject test SSE manager
            var testSse = new TestSSEManager("https://example.com/sse", (s,a)=>{}, (s,a)=>{}, (s,a)=>{});
            manager.SetSseManager(testSse);
            return manager;
        }

        [TestMethod]
        public void OpenStateSetsSsePollingInterval()
        {
            var manager = BuildManager();
            manager.TestInvokeSseState(ReadyState.Open);
            Assert.AreEqual(15 * 60 * 1000, manager.CurrentPollingIntervalMs, "Polling interval should switch to 15 minutes on Open state");
        }

        [TestMethod]
        public void ErrorAfterOpenRevertsToBaseInterval()
        {
            var baseInterval = 4000;
            var manager = BuildManager(baseInterval);
            manager.TestInvokeSseState(ReadyState.Open);
            Assert.AreEqual(15 * 60 * 1000, manager.CurrentPollingIntervalMs);
            manager.TestInvokeSseError();
            Assert.AreEqual(baseInterval, manager.CurrentPollingIntervalMs, "Polling interval should revert to base on error");
            Assert.AreEqual(1, manager.ConsecutiveSseErrorCount, "Error count should be incremented");
        }

        [TestMethod]
        public void ErrorsTriggerRestartAfterThreshold()
        {
            var manager = BuildManager();
            var testSse = new TestSSEManager("https://example.com/sse", (s,a)=>{}, (s,a)=>{}, (s,a)=>{});
            manager.SetSseManager(testSse);
            // simulate errors up to threshold (5)
            for (int i = 0; i < 5; i++)
            {
                manager.TestInvokeSseError();
            }
            Assert.AreEqual(1, testSse.RestartCalls, "Restart should be called once after threshold errors");
            Assert.AreEqual(0, manager.ConsecutiveSseErrorCount, "Error counter should reset after restart");
        }

        [TestMethod]
        public void MultipleThresholdsCauseMultipleRestarts()
        {
            var manager = BuildManager();
            var testSse = new TestSSEManager("https://example.com/sse", (s,a)=>{}, (s,a)=>{}, (s,a)=>{});
            manager.SetSseManager(testSse);
            for (int cycle = 0; cycle < 2; cycle++)
            {
                for (int i = 0; i < 5; i++)
                {
                    manager.TestInvokeSseError();
                }
            }
            Assert.AreEqual(2, testSse.RestartCalls, "Restart should be called for each threshold cycle");
        }

        [TestMethod]
        public void ClosedStateRevertsIntervalToBase()
        {
            var manager = BuildManager();
            manager.TestInvokeSseState(ReadyState.Open);
            Assert.AreEqual(15 * 60 * 1000, manager.CurrentPollingIntervalMs);
            manager.TestInvokeSseState(ReadyState.Closed);
            Assert.AreEqual(5000, manager.CurrentPollingIntervalMs, "Closed state should revert to base interval");
        }
    }
}

