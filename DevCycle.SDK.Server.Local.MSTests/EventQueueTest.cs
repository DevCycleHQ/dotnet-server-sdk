using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Local.Api;
using DevCycle.SDK.Server.Common.API;
using DevCycle.SDK.Server.Common.Model;
using DevCycle.SDK.Server.Common.Model.Local;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using RestSharp;
using RichardSzalay.MockHttp;

namespace DevCycle.SDK.Server.Local.MSTests
{
    [TestClass]
    public class EventQueueTest
    {
        private Tuple<EventQueue, MockHttpMessageHandler, MockedRequest> getTestQueue(bool isError = false,
            bool isRetryableError = false, LogLevel logLevel = LogLevel.Information,
            DevCycleLocalOptions localOptions = null)
        {
            var mockHttp = new MockHttpMessageHandler();
            var sdkKey = $"server-{Guid.NewGuid()}";
            var statusCode =
                isError
                    ? isRetryableError
                        ? HttpStatusCode.InternalServerError
                        : HttpStatusCode.BadRequest
                    : HttpStatusCode.Created;
            MockedRequest
                req = mockHttp.When("https://*")
                    .Respond(statusCode,
                        "application/json",
                        "{}");
            localOptions ??= new DevCycleLocalOptions(10, 10);
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(logLevel);
            });
            var localBucketing = new WASMLocalBucketing();
            string config = new string(Fixtures.Config());
            localBucketing.StoreConfig(sdkKey, config);
            localBucketing.SetPlatformData(JsonConvert.SerializeObject(new PlatformData()));

            var eventQueue = new EventQueue(sdkKey, localOptions, loggerFactory,
                localBucketing, new DevCycleRestClientOptions() { ConfigureMessageHandler = _ => mockHttp });
            return new Tuple<EventQueue, MockHttpMessageHandler, MockedRequest>(eventQueue, mockHttp, req);
        }

        private async Task WaitForOneEvent(EventQueue eventQueue)
        {
            var completion = new ManualResetEvent(false);
            eventQueue.AddFlushedEventsSubscriber((object sender, DevCycleEventArgs e) =>
            {
                completion.Set();
            });
            await eventQueue.FlushEvents();
            completion.WaitOne();
        }

        private void QueueSimpleEvent(EventQueue eventQueue, DevCycleEvent @event = null)
        {
            @event ??= new DevCycleEvent("testEvent", metaData: new Dictionary<string, object> { { "test", "value" } });
            var user = new DevCycleUser("1");

            var dvcPopulatedUser = new DevCyclePopulatedUser(user);
            eventQueue.QueueEvent(dvcPopulatedUser, @event);
        }
        private void QueueSimpleAggregateEvent(EventQueue eventQueue, DevCycleEvent @event = null)
        {
            @event ??= new DevCycleEvent(
                "aggVariableEvaluated",
                Fixtures.VariableKey,
                metaData: new Dictionary<string, object> { { "test", "value" } });
            var user = new DevCycleUser("1");
            
            var config = new BucketedUserConfig
            {
                FeatureVariationMap = new Dictionary<string, string> { { Fixtures.FeatureId, Fixtures.VariationOnId } },
                VariableVariationMap = new Dictionary<string, FeatureVariation>
                {
                    [Fixtures.VariableKey] = new FeatureVariation
                    {
                        Feature = Fixtures.FeatureId,
                        Variation = Fixtures.VariationOnId
                    },
                }
            };

            var dvcPopulatedUser = new DevCyclePopulatedUser(user);
            eventQueue.QueueAggregateEvent(dvcPopulatedUser, @event, config);
        }

        [TestMethod]
        public async Task FlushSuccessfulCustomEvent()
        {
            var (eventQueue, messageHandler, request) = getTestQueue();
            QueueSimpleEvent(eventQueue);

            eventQueue.AddFlushedEventsSubscriber(AssertSuccessfulEvent);
            await WaitForOneEvent(eventQueue);
            Assert.AreEqual(1, messageHandler.GetMatchCount(request));
        }
        
        [TestMethod]
        public async Task FlushSuccessfulAggregateEvent()
        {
            var (eventQueue, messageHandler, request) = getTestQueue();
            QueueSimpleAggregateEvent(eventQueue);

            eventQueue.AddFlushedEventsSubscriber(AssertSuccessfulEvent);
            await WaitForOneEvent(eventQueue);
            Assert.AreEqual(1, messageHandler.GetMatchCount(request));
        }
        
        [TestMethod]
        public async Task FlushFailedCustomEvent()
        {
            var (eventQueue, messageHandler, request) = getTestQueue(true);
            QueueSimpleEvent(eventQueue);

            eventQueue.AddFlushedEventsSubscriber(AssertFailedEvent);
            await WaitForOneEvent(eventQueue);
            Assert.AreEqual(1, messageHandler.GetMatchCount(request));
        }
        
        [TestMethod]
        public async Task FlushFailedAggregateEvent()
        {
            var (eventQueue, messageHandler, request) = getTestQueue(true);
            QueueSimpleAggregateEvent(eventQueue);

            eventQueue.AddFlushedEventsSubscriber(AssertFailedEvent);
            await WaitForOneEvent(eventQueue);
            Assert.AreEqual(1, messageHandler.GetMatchCount(request));
        }

        [TestMethod]
        public async Task FailAndRetry()
        {
            var (eventQueue, messageHandler, request) = getTestQueue(true, true);
            QueueSimpleEvent(eventQueue);
            await WaitForOneEvent(eventQueue);
            Assert.AreEqual(1, messageHandler.GetMatchCount(request));
            await eventQueue.FlushEvents();
            Assert.AreEqual(2, messageHandler.GetMatchCount(request));
        }

        [TestMethod]
        public async Task FailAndDoNoRetry()
        {
            var (eventQueue, messageHandler, request) = getTestQueue(true, false);
            QueueSimpleEvent(eventQueue);
            await WaitForOneEvent(eventQueue);
            Assert.AreEqual(1, messageHandler.GetMatchCount(request));
            await eventQueue.FlushEvents();
            Assert.AreEqual(1, messageHandler.GetMatchCount(request));
        }

        [TestMethod]
        public async Task UseMultipleThreads()
        {
            var (eventQueue, messageHandler, request) = getTestQueue();

            var tasks = new List<Task>();
            for (var i = 0; i<10; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    QueueSimpleEvent(eventQueue);
                }));
            }
            await Task.WhenAll(tasks);
            await WaitForOneEvent(eventQueue);
            Assert.AreEqual(1, messageHandler.GetMatchCount(request));
        }

        private void AssertSuccessfulEvent(object sender, DevCycleEventArgs e)
        {
            Assert.AreEqual(0, e.Errors.Count);
            Assert.IsTrue(e.Success);
        }

        private void AssertFailedEvent(object sender, DevCycleEventArgs e)
        {
            Assert.AreNotEqual(0, e.Errors.Count);
            Assert.IsFalse(e.Success);
        }

        // ---------------------------------------------------------------
        // Tests for the FlushMutex leak fix.
        //
        // Background: when the WASM bucketing engine traps inside
        // flushEventQueue (e.g. its event queue state is corrupted from
        // accumulated AssemblyScript throws), the previous implementation
        // of FlushEvents() let the exception escape AFTER calling
        // localBucketing.StartFlush() but BEFORE calling EndFlush(). That
        // permanently leaked the FlushMutex and every subsequent flush
        // would deadlock, causing events to accumulate in the WASM heap
        // indefinitely. The fix wraps StartFlush/EndFlush in try/finally
        // and swallows WASM exceptions so the mutex is always released.
        // ---------------------------------------------------------------

        [TestMethod]
        public async Task FlushEvents_WhenFlushEventQueueThrows_DoesNotPropagate()
        {
            var bucketing = new TrappingLocalBucketing(throwOnFlushEventQueue: true);
            var eventQueue = BuildEventQueueWithFakeBucketing(bucketing);

            // Should NOT throw - the fix swallows the exception so the
            // mutex outer try/finally can release the FlushMutex.
            await eventQueue.FlushEvents();
        }

        [TestMethod]
        public async Task FlushEvents_WhenFlushEventQueueThrows_StillCallsEndFlush()
        {
            var bucketing = new TrappingLocalBucketing(throwOnFlushEventQueue: true);
            var eventQueue = BuildEventQueueWithFakeBucketing(bucketing);

            await eventQueue.FlushEvents();

            Assert.AreEqual(1, bucketing.StartFlushCount,
                "StartFlush should have been called exactly once.");
            Assert.AreEqual(1, bucketing.EndFlushCount,
                "EndFlush MUST be called even when FlushEventQueue throws, "
                + "otherwise the FlushMutex semaphore is permanently leaked.");
        }

        [TestMethod]
        public async Task FlushEvents_WhenFlushEventQueueThrows_RaisesFailureToSubscribers()
        {
            var bucketing = new TrappingLocalBucketing(throwOnFlushEventQueue: true);
            var eventQueue = BuildEventQueueWithFakeBucketing(bucketing);

            DevCycleEventArgs received = null;
            var completion = new ManualResetEvent(false);
            eventQueue.AddFlushedEventsSubscriber((sender, e) =>
            {
                received = e;
                completion.Set();
            });

            await eventQueue.FlushEvents();
            completion.WaitOne(TimeSpan.FromSeconds(2));

            Assert.IsNotNull(received, "FlushedEvents subscriber should have been invoked.");
            Assert.IsFalse(received.Success);
            Assert.IsTrue(received.Errors.Count > 0,
                "Errors collection should contain the underlying exception.");
        }

        [TestMethod]
        public async Task FlushEvents_AfterFailedFlush_NextFlushDoesNotDeadlock()
        {
            // This test would hang forever before the fix: the first flush
            // throws inside FlushEventQueue, EndFlush never runs, FlushMutex
            // is leaked, and the second flush blocks on StartFlush forever.
            var bucketing = new TrappingLocalBucketing(throwOnFlushEventQueue: true);
            var eventQueue = BuildEventQueueWithFakeBucketing(bucketing);

            await eventQueue.FlushEvents();

            // Now turn off the fault and try again. With the fix, this
            // completes promptly. Without the fix, this deadlocks.
            bucketing.ThrowOnFlushEventQueue = false;
            var second = eventQueue.FlushEvents();
            var completed = await Task.WhenAny(second, Task.Delay(TimeSpan.FromSeconds(5)));

            Assert.AreSame(second, completed,
                "Second flush should complete instead of deadlocking on a leaked FlushMutex.");
            Assert.AreEqual(2, bucketing.StartFlushCount);
            Assert.AreEqual(2, bucketing.EndFlushCount);
        }

        private EventQueue BuildEventQueueWithFakeBucketing(TrappingLocalBucketing bucketing)
        {
            var sdkKey = $"server-{Guid.NewGuid()}";
            var loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.None));
            var options = new DevCycleLocalOptions(10, 10);

            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When("https://*").Respond(HttpStatusCode.Created, "application/json", "{}");

            return new EventQueue(sdkKey, options, loggerFactory, bucketing,
                new DevCycleRestClientOptions { ConfigureMessageHandler = _ => mockHttp });
        }

        /// <summary>
        /// Test fake of <see cref="ILocalBucketing"/> that simulates a WASM
        /// trap inside <c>FlushEventQueue</c>. Tracks Start/EndFlush call
        /// counts so tests can assert the FlushMutex was released.
        /// </summary>
        private class TrappingLocalBucketing : ILocalBucketing
        {
            public bool ThrowOnFlushEventQueue { get; set; }
            public int StartFlushCount { get; private set; }
            public int EndFlushCount { get; private set; }

            public TrappingLocalBucketing(bool throwOnFlushEventQueue)
            {
                ThrowOnFlushEventQueue = throwOnFlushEventQueue;
            }

            public string ClientUUID => "test-client-uuid";

            public List<FlushPayload> FlushEventQueue(string sdkKey)
            {
                if (ThrowOnFlushEventQueue)
                {
                    // Mimics a wasmtime trap surfaced as LocalBucketingException.
                    throw new LocalBucketingException("Simulated WASM trap during flushEventQueue");
                }
                return new List<FlushPayload>();
            }

            public void StartFlush() { StartFlushCount++; }
            public void EndFlush() { EndFlushCount++; }

            // Unused on the flush path - return safe defaults / no-ops.
            public void InitEventQueue(string sdkKey, string options) { }
            public BucketedUserConfig GenerateBucketedConfig(string sdkKey, string user) =>
                new BucketedUserConfig();
            public int EventQueueSize(string sdkKey) => 0;
            public void QueueEvent(string sdkKey, string user, string eventString) { }
            public void QueueAggregateEvent(string sdkKey, string eventString, string variableVariationMapStr) { }
            public void OnPayloadSuccess(string sdkKey, string payloadId) { }
            public void OnPayloadFailure(string sdkKey, string payloadId, bool retryable) { }
            public void StoreConfig(string sdkKey, string config) { }
            public void SetPlatformData(string platformData) { }
            public string GetVariable(string sdkKey, string userJSON, string key, TypeEnum variableType, bool shouldTrackEvent) => null;
            public string GetConfigMetadata(string sdkKey) => null;
            public byte[] GetVariableForUserProtobuf(byte[] serializedParams) => null;
            public void SetClientCustomData(string sdkKey, string customData) { }
        }
    }
}