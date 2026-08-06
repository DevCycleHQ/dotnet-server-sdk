using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Common.API;
using DevCycle.SDK.Server.Common.Model;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenFeature.Model;

namespace DevCycle.SDK.Server.Cloud.MSTests
{
    /// <summary>
    /// Covers the fire-and-forget contract of <see cref="DevCycleProvider.Track"/>. The method
    /// must never throw at the call site and must never let an exception escape onto the thread
    /// pool, which is what an <c>async void</c> implementation would do (terminating the process).
    /// </summary>
    [TestClass]
    public class DevCycleProviderTrackTest
    {
        private static readonly TimeSpan LogWaitTimeout = TimeSpan.FromSeconds(5);

        [TestMethod]
        public void Track_WhenClientTrackThrows_DoesNotThrowAndLogsWarning()
        {
            var logger = new CapturingLogger();
            var client = new ThrowingTrackClient(new InvalidOperationException("track boom"));
            var provider = new DevCycleProvider(client, logger);

            var context = EvaluationContext.Builder().SetTargetingKey("user-1").Build();

            provider.Track("my-event", context, TrackingEventDetails.Empty);

            var entry = WaitForWarning(logger);
            Assert.IsNotNull(entry, "expected a logged warning for the failed tracking call");
            StringAssert.Contains(entry.Message, "my-event");
            Assert.IsInstanceOfType(entry.Exception, typeof(InvalidOperationException));
        }

        [TestMethod]
        public void Track_WithNullEvaluationContext_DoesNotThrowAndLogsWarning()
        {
            var logger = new CapturingLogger();
            var client = new RecordingTrackClient();
            var provider = new DevCycleProvider(client, logger);

            provider.Track("my-event", null, TrackingEventDetails.Empty);

            var entry = WaitForWarning(logger);
            Assert.IsNotNull(entry, "expected a logged warning for the null evaluation context");
            Assert.AreEqual(0, client.CallCount, "client should not be called when context conversion fails");
        }

        [TestMethod]
        public void Track_WithContextMissingTargetingKey_DoesNotThrowAndLogsWarning()
        {
            var logger = new CapturingLogger();
            var client = new RecordingTrackClient();
            var provider = new DevCycleProvider(client, logger);

            var context = EvaluationContext.Builder().Set("email", "test@example.com").Build();

            provider.Track("my-event", context, TrackingEventDetails.Empty);

            var entry = WaitForWarning(logger);
            Assert.IsNotNull(entry, "expected a logged warning for the missing targeting key");
            Assert.AreEqual(0, client.CallCount, "client should not be called when context conversion fails");
        }

        [TestMethod]
        public void Track_WithNullTrackingEventDetails_DoesNotThrowAndLogsWarning()
        {
            var logger = new CapturingLogger();
            var client = new RecordingTrackClient();
            var provider = new DevCycleProvider(client, logger);

            var context = EvaluationContext.Builder().SetTargetingKey("user-1").Build();

            provider.Track("my-event", context, null);

            var entry = WaitForWarning(logger);
            Assert.IsNotNull(entry, "expected a logged warning for the null tracking event details");
            Assert.AreEqual(0, client.CallCount, "client should not be called when event conversion fails");
        }

        [TestMethod]
        public void Track_WithNullLogger_DoesNotThrow()
        {
            var client = new ThrowingTrackClient(new InvalidOperationException("track boom"));
            var provider = new DevCycleProvider(client);

            var context = EvaluationContext.Builder().SetTargetingKey("user-1").Build();

            // A provider built without a logger must still swallow the failure rather than
            // faulting the thread pool.
            provider.Track("my-event", context, TrackingEventDetails.Empty);

            Assert.IsTrue(WaitFor(() => client.CallCount == 1), "expected the client to be invoked");
        }

        [TestMethod]
        public void Track_OnSuccess_ForwardsEventNameAndUserToClient()
        {
            var logger = new CapturingLogger();
            var client = new RecordingTrackClient();
            var provider = new DevCycleProvider(client, logger);

            var context = EvaluationContext.Builder().SetTargetingKey("user-1").Build();

            provider.Track("my-event", context, TrackingEventDetails.Empty);

            Assert.IsTrue(WaitFor(() => client.CallCount == 1), "expected the client to be invoked");
            Assert.AreEqual("my-event", client.LastEvent.Type);
            Assert.AreEqual("user-1", client.LastUser.UserId);
            Assert.AreEqual(0, logger.Entries.Count, "no warnings expected on the success path");
        }

        private static LogEntry WaitForWarning(CapturingLogger logger)
        {
            WaitFor(() => logger.Entries.Count > 0);
            return logger.Entries.FirstOrDefault(entry => entry.Level == LogLevel.Warning);
        }

        private static bool WaitFor(Func<bool> condition)
        {
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed < LogWaitTimeout)
            {
                if (condition()) return true;
                Task.Delay(10).Wait();
            }

            return condition();
        }

        private class LogEntry
        {
            public LogLevel Level { get; set; }
            public string Message { get; set; }
            public Exception Exception { get; set; }
        }

        private class CapturingLogger : ILogger
        {
            private readonly List<LogEntry> entries = new List<LogEntry>();

            public IReadOnlyList<LogEntry> Entries
            {
                get
                {
                    lock (entries)
                    {
                        return entries.ToList();
                    }
                }
            }

            public IDisposable BeginScope<TState>(TState state) => new NoopScope();

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
                Func<TState, Exception, string> formatter)
            {
                lock (entries)
                {
                    entries.Add(new LogEntry
                    {
                        Level = logLevel,
                        Message = formatter(state, exception),
                        Exception = exception
                    });
                }
            }

            private class NoopScope : IDisposable
            {
                public void Dispose()
                {
                }
            }
        }

        /// <summary>
        /// Minimal <see cref="DevCycleBaseClient"/> stub; only <c>Track</c> is exercised.
        /// </summary>
        private abstract class StubClient : DevCycleBaseClient
        {
            public override void Dispose()
            {
            }

            public override string Platform() => "Test";

            public override IDevCycleApiClient GetApiClient() => throw new NotImplementedException();

            public override DevCycleProvider GetOpenFeatureProvider() => throw new NotImplementedException();

            public override Task<Dictionary<string, Feature>> AllFeatures(DevCycleUser user) =>
                throw new NotImplementedException();

            public override Task<Dictionary<string, ReadOnlyVariable<object>>> AllVariables(DevCycleUser user) =>
                throw new NotImplementedException();

            public override Task<Variable<T>> Variable<T>(DevCycleUser user, string key, T defaultValue) =>
                throw new NotImplementedException();

            public override Task<T> VariableValue<T>(DevCycleUser user, string key, T defaultValue) =>
                throw new NotImplementedException();
        }

        private class ThrowingTrackClient : StubClient
        {
            private readonly Exception toThrow;

            public ThrowingTrackClient(Exception toThrow)
            {
                this.toThrow = toThrow;
            }

            public int CallCount { get; private set; }

            public override Task<DevCycleResponse> Track(DevCycleUser user, DevCycleEvent userEvent)
            {
                CallCount++;
                throw toThrow;
            }
        }

        private class RecordingTrackClient : StubClient
        {
            public int CallCount { get; private set; }
            public DevCycleUser LastUser { get; private set; }
            public DevCycleEvent LastEvent { get; private set; }

            public override Task<DevCycleResponse> Track(DevCycleUser user, DevCycleEvent userEvent)
            {
                LastUser = user;
                LastEvent = userEvent;
                CallCount++;
                return Task.FromResult(new DevCycleResponse("ok"));
            }
        }
    }
}
