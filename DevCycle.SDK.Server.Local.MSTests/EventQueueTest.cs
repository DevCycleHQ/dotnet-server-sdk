using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Local.Api;
using DevCycle.SDK.Server.Common.Model;
using DevCycle.SDK.Server.Common.Model.Local;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RestSharp;
using RichardSzalay.MockHttp;

namespace DevCycle.SDK.Server.Local.MSTests
{
    [TestClass]
    public class EventQueueTest
    {
        
        private Tuple<EventQueue, MockHttpMessageHandler, MockedRequest> getTestQueue(bool isError = false,
            bool isRetryableError = false)
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
            var localOptions = new DVCLocalOptions(10, 10);
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

            
            var eventQueue = new EventQueue(environmentKey, localOptions, loggerFactory,
                new RestClientOptions() {ConfigureMessageHandler = _ => mockHttp});
            return new Tuple<EventQueue, MockHttpMessageHandler, MockedRequest>(eventQueue, mockHttp, req);
        }

        [TestMethod]
        public async Task FlushEvents_EventQueuedAndFlushed_OnCallBackIsSuccessful()
        {
            var eventQueue = getTestQueue();
            eventQueue.Item1.AddFlushedEventsSubscriber(AssertTrueFlushedEvents);

            var user = new User("1");

            var @event = new Event("testEvent", metaData: new Dictionary<string, object> {{"test", "value"}});

            var config = new BucketedUserConfig
            {
                FeatureVariationMap = new Dictionary<string, string> {{"some-feature-id", "some-variation-id"}}
            };

            var dvcPopulatedUser = new DVCPopulatedUser(user);
            eventQueue.Item1.QueueEvent(dvcPopulatedUser, @event, config);

            await eventQueue.Item1.FlushEvents();

            await Task.Delay(20);
        }

        [TestMethod]
        public async Task FlushEvents_EventQueuedAndFlushed_OnCallBackIsSuccessful_VerifyFlushEventsCalledOnce()
        {
            var eventQueue = getTestQueue();

            eventQueue.Item1.AddFlushedEventsSubscriber(AssertTrueFlushedEvents);

            var user = new User(userId: "1");

            var @event = new Event("testEvent", metaData: new Dictionary<string, object> {{"test", "value"}});

            var config = new BucketedUserConfig
            {
                FeatureVariationMap = new Dictionary<string, string> {{"some-feature-id", "some-variation-id"}}
            };

            var dvcPopulatedUser = new DVCPopulatedUser(user);
            eventQueue.Item1.QueueEvent(dvcPopulatedUser, @event, config);

            await eventQueue.Item1.FlushEvents();

            await Task.Delay(1000);

            Assert.AreEqual(1, eventQueue.Item2.GetMatchCount(eventQueue.Item3));
        }

        [TestMethod]
        public async Task FlushEvents_EventQueuedAndFlushed_QueueNotFlushedOnFirstAttempt_VerifyFlushEventsCalledTwice()
        {
            var eventsQueue = getTestQueue(true, true);

            eventsQueue.Item1.AddFlushedEventsSubscriber(AssertFalseFlushedEvents);

            var user = new User(userId: "1");

            var @event = new Event("testEvent", metaData: new Dictionary<string, object> {{"test", "value"}});

            var config = new BucketedUserConfig
            {
                FeatureVariationMap = new Dictionary<string, string> {{"some-feature-id", "some-variation-id"}}
            };

            var dvcPopulatedUser = new DVCPopulatedUser(user);
            eventsQueue.Item1.QueueEvent(dvcPopulatedUser, @event, config);

            await eventsQueue.Item1.FlushEvents();

            Assert.AreEqual(1, eventsQueue.Item2.GetMatchCount(eventsQueue.Item3));

            // add delay so the queue should still be looping trying to flush
            await Task.Delay(100);
            var retryCount = eventsQueue.Item2.GetMatchCount(eventsQueue.Item3);

            Assert.IsTrue(retryCount >= 1);

            // ensure the queue can now be flushed

            eventsQueue.Item3.Respond(HttpStatusCode.Created);

            eventsQueue.Item1.RemoveFlushedEventsSubscriber(AssertFalseFlushedEvents);
            eventsQueue.Item1.AddFlushedEventsSubscriber(AssertTrueFlushedEvents);

            // Add a longer delay to the test to ensure FlushEvents is no longer looping
            await Task.Delay(500);

            Assert.AreEqual(retryCount, eventsQueue.Item2.GetMatchCount(eventsQueue.Item3));

            // internal event queue should now be empty, flush events manually and check that publish isnt called
            await eventsQueue.Item1.FlushEvents();
            await Task.Delay(20);
            Assert.AreEqual(1, eventsQueue.Item2.GetMatchCount(eventsQueue.Item3));
        }

        [TestMethod]
        public async Task FlushEvents_EventQueuedAndFlushed_QueueNotFlushedNonRetryable_VerifyFlushEventsCalledOnce()
        {
            var eventsQueue = getTestQueue(true);


            eventsQueue.Item1.AddFlushedEventsSubscriber(AssertFalseFlushedEvents);

            var user = new User(userId: "1");

            var @event = new Event("testEvent", metaData: new Dictionary<string, object> {{"test", "value"}});

            var config = new BucketedUserConfig
            {
                FeatureVariationMap = new Dictionary<string, string> {{"some-feature-id", "some-variation-id"}}
            };

            var dvcPopulatedUser = new DVCPopulatedUser(user);
            eventsQueue.Item1.QueueEvent(dvcPopulatedUser, @event, config);

            await eventsQueue.Item1.FlushEvents();

            Assert.AreEqual(1, eventsQueue.Item2.GetMatchCount(eventsQueue.Item3));

            // add delay to make sure we purged events that failed and were non-retryable, thus haven't flushed again
            await Task.Delay(500);
            Assert.AreEqual(1, eventsQueue.Item2.GetMatchCount(eventsQueue.Item3));
        }

        [TestMethod]
        public async Task QueueAggregateEvents_EventsQueuedSuccessfully()
        {
            // Mock EventQueue for verification without mocking any methods
            var eventsQueue = getTestQueue();

            var dvcPopulatedUser = new DVCPopulatedUser(new User(userId: "1", name: "User1"));
            var dvcPopulatedUser2 = new DVCPopulatedUser(new User(userId: "2", name: "User2"));
            var dvcPopulatedUser3 = new DVCPopulatedUser(new User(userId: "1", name: "User3"));

            var @event = new Event("variableEvaluated", target: "var1");
            var @event2 = new Event(type: "variableEvaluated", target: "var2");
            var @event3 = new Event(type: "variableDefaulted", target: "var2");

            var config = new BucketedUserConfig
            {
                FeatureVariationMap = new Dictionary<string, string> {{"some-feature-id", "some-variation-id"}}
            };

            var configIdentical = new BucketedUserConfig
            {
                FeatureVariationMap = new Dictionary<string, string> {{"some-feature-id", "some-variation-id"}}
            };

            var config2 = new BucketedUserConfig
            {
                FeatureVariationMap = new Dictionary<string, string> {{"feature2", "variation2"}}
            };

            eventsQueue.Item1.QueueAggregateEvent(dvcPopulatedUser, @event, config);
            eventsQueue.Item1.QueueAggregateEvent(dvcPopulatedUser, @event, configIdentical);
            eventsQueue.Item1.QueueAggregateEvent(dvcPopulatedUser, @event2, config);
            eventsQueue.Item1.QueueAggregateEvent(dvcPopulatedUser, @event2, config);
            eventsQueue.Item1.QueueAggregateEvent(dvcPopulatedUser, @event3, config);
            eventsQueue.Item1.QueueAggregateEvent(dvcPopulatedUser, @event, config2);
            eventsQueue.Item1.QueueAggregateEvent(dvcPopulatedUser2, @event, config);
            eventsQueue.Item1.QueueAggregateEvent(dvcPopulatedUser3, @event, config);

            eventsQueue.Item1.AddFlushedEventsSubscriber(AssertTrueFlushedEvents);

            await eventsQueue.Item1.FlushEvents();
            await Task.Delay(20);

            Assert.AreEqual(1, eventsQueue.Item2.GetMatchCount(eventsQueue.Item3));

            await eventsQueue.Item1.FlushEvents();
            await Task.Delay(20);
            // verify that it hasn't been called again because there should be nothing in the queue
            Assert.AreEqual(1, eventsQueue.Item2.GetMatchCount(eventsQueue.Item3));

        }

        private void AssertTrueFlushedEvents(object sender, DVCEventArgs e)
        {
            Assert.IsTrue(e.Success);
        }

        private void AssertFalseFlushedEvents(object sender, DVCEventArgs e)
        {
            Assert.IsFalse(e.Success);
        }

        private bool AssertAggregateEventsBatch(BatchOfUserEventsBatch b)
        {
            Assert.IsTrue(b.UserEventsBatchRecords.Count == 3);
            Assert.IsTrue(b.UserEventsBatchRecords[0].User.Name == "User1");
            Assert.IsTrue(b.UserEventsBatchRecords[0].Events.Count == 4);

            Assert.IsTrue(b.UserEventsBatchRecords[0].Events[0].Type == "variableEvaluated");
            Assert.IsTrue(b.UserEventsBatchRecords[0].Events[0].Target == "var1");
            Assert.IsTrue(b.UserEventsBatchRecords[0].Events[0].Value == 2);

            Assert.IsTrue(b.UserEventsBatchRecords[0].Events[1].Type == "variableEvaluated");
            Assert.IsTrue(b.UserEventsBatchRecords[0].Events[1].Target == "var2");
            Assert.IsTrue(b.UserEventsBatchRecords[0].Events[1].Value == 2);

            Assert.IsTrue(b.UserEventsBatchRecords[0].Events[2].Type == "variableDefaulted");
            Assert.IsTrue(b.UserEventsBatchRecords[0].Events[2].Target == "var2");
            Assert.IsTrue(b.UserEventsBatchRecords[0].Events[2].Value == 1);

            Assert.IsTrue(b.UserEventsBatchRecords[0].Events[3].Type == "variableEvaluated");
            Assert.IsTrue(b.UserEventsBatchRecords[0].Events[3].Target == "var1");
            Assert.IsTrue(b.UserEventsBatchRecords[0].Events[3].Value == 1);
            Assert.IsTrue(b.UserEventsBatchRecords[0].Events[3].FeatureVars["feature2"] == "variation2");

            Assert.IsTrue(b.UserEventsBatchRecords[1].User.Name == "User2");
            Assert.IsTrue(b.UserEventsBatchRecords[1].Events.Count == 1);
            Assert.IsTrue(b.UserEventsBatchRecords[1].Events[0].Type == "variableEvaluated");
            Assert.IsTrue(b.UserEventsBatchRecords[1].Events[0].Target == "var1");
            Assert.IsTrue(b.UserEventsBatchRecords[1].Events[0].Value == 1);

            Assert.IsTrue(b.UserEventsBatchRecords[2].User.Name == "User3");
            Assert.IsTrue(b.UserEventsBatchRecords[2].Events.Count == 1);
            Assert.IsTrue(b.UserEventsBatchRecords[2].Events[0].Type == "variableEvaluated");
            Assert.IsTrue(b.UserEventsBatchRecords[2].Events[0].Target == "var1");
            Assert.IsTrue(b.UserEventsBatchRecords[2].Events[0].Value == 1);

            return true;
        }
    }
}