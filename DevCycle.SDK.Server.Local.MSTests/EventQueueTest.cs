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
            DVCLocalOptions localOptions = null)
        {
            var mockHttp = new MockHttpMessageHandler();
            var environmentKey = $"server-{Guid.NewGuid()}";
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
            localOptions ??= new DVCLocalOptions(10, 10);
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(logLevel);
            });
            var localBucketing = new LocalBucketing();
            string config = new string(Fixtures.Config());
            localBucketing.StoreConfig(environmentKey, config);
            localBucketing.SetPlatformData(JsonConvert.SerializeObject(new PlatformData()));

            var eventQueue = new EventQueue(environmentKey, localOptions, loggerFactory,
                localBucketing, new DVCRestClientOptions() { ConfigureMessageHandler = _ => mockHttp });
            return new Tuple<EventQueue, MockHttpMessageHandler, MockedRequest>(eventQueue, mockHttp, req);
        }

        private async Task WaitForOneEvent(EventQueue eventQueue)
        {
            var completion = new ManualResetEvent(false);
            eventQueue.AddFlushedEventsSubscriber((object sender, DVCEventArgs e) =>
            {
                completion.Set();
            });
            await eventQueue.FlushEvents();
            completion.WaitOne();
        }

        private void QueueSimpleEvent(EventQueue eventQueue, Event @event = null)
        {
            @event ??= new Event("testEvent", metaData: new Dictionary<string, object> { { "test", "value" } });
            var user = new User("1");

            var dvcPopulatedUser = new DVCPopulatedUser(user);
            eventQueue.QueueEvent(dvcPopulatedUser, @event);
        }
        private void QueueSimpleAggregateEvent(EventQueue eventQueue, Event @event = null)
        {
            @event ??= new Event(
                "aggVariableEvaluated",
                Fixtures.VariableKey,
                metaData: new Dictionary<string, object> { { "test", "value" } });
            var user = new User("1");
            
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

            var dvcPopulatedUser = new DVCPopulatedUser(user);
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

        private void AssertSuccessfulEvent(object sender, DVCEventArgs e)
        {
            Assert.AreEqual(0, e.Errors.Count);
            Assert.IsTrue(e.Success);
        }

        private void AssertFailedEvent(object sender, DVCEventArgs e)
        {
            Assert.AreNotEqual(0, e.Errors.Count);
            Assert.IsFalse(e.Success);
        }
    }
}