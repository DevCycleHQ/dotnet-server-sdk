using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Local.Api;
using DevCycle.SDK.Server.Common.Model;
using DevCycle.SDK.Server.Common.Model.Local;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RestSharp.Portable;

namespace DevCycle.SDK.Server.Local.MSTests
{
    [TestClass]
    public class EventQueueTest
    {
        private Mock<DVCEventsApiClient> dvcEventsApiClient;

        private DVCOptions options;
        private EventQueue eventQueue;
        private ILoggerFactory loggerFactory;
        
        [TestInitialize]
        public void BeforeEachTest()
        {
            dvcEventsApiClient = new Mock<DVCEventsApiClient>();
            options = new DVCOptions(10, 10);
            loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        }
        
        [TestMethod]
        public async Task FlushEvents_EventQueuedAndFlushed_OnCallBackIsSuccessful()
        {
            var mockResponse = new Mock<IRestResponse>();
            mockResponse.SetupGet(_ => _.StatusCode).Returns(HttpStatusCode.Created);

            dvcEventsApiClient.Setup(m => m.PublishEvents(It.IsAny<BatchOfUserEventsBatch>()))
                .ReturnsAsync(mockResponse.Object);
            
            eventQueue = new EventQueue("some-key", options, loggerFactory, null);
            eventQueue.SetPrivateFieldValue("dvcEventsApiClient", dvcEventsApiClient.Object);
            
            eventQueue.AddFlushedEventsSubscriber(AssertTrueFlushedEvents);

            var user = new User(userId: "1");

            var @event = new Event("testEvent", metaData: new Dictionary<string, object> {{"test", "value"}});

            var config = new BucketedUserConfig
            {
                FeatureVariationMap = new Dictionary<string, string> {{"some-feature-id", "some-variation-id"}}
            };

            var dvcPopulatedUser = new DVCPopulatedUser(user);
            eventQueue.QueueEvent(dvcPopulatedUser, @event, config);

            await eventQueue.FlushEvents();

            await Task.Delay(20);
        }

        [TestMethod]
        public async Task FlushEvents_EventQueuedAndFlushed_OnCallBackIsSuccessful_VerifyFlushEventsCalledOnce()
        {
            var mockedQueue = new Mock<EventQueue>
            {
                CallBase = true
            };
            
            var mockResponse = new Mock<IRestResponse>();
            mockResponse.SetupGet(_ => _.StatusCode).Returns(HttpStatusCode.Created);
            
            dvcEventsApiClient.Setup(m => m.PublishEvents(It.IsAny<BatchOfUserEventsBatch>()))
                .ReturnsAsync(mockResponse.Object);
            mockedQueue.Object.SetPrivateFieldValue("dvcEventsApiClient", dvcEventsApiClient.Object);
            
            mockedQueue.Object.AddFlushedEventsSubscriber(AssertTrueFlushedEvents);

            var user = new User(userId: "1");

            var @event = new Event("testEvent", metaData: new Dictionary<string, object> {{"test", "value"}});

            var config = new BucketedUserConfig
            {
                FeatureVariationMap = new Dictionary<string, string> {{"some-feature-id", "some-variation-id"}}
            };

            var dvcPopulatedUser = new DVCPopulatedUser(user);
            mockedQueue.Object.QueueEvent(dvcPopulatedUser, @event, config);
            
            await mockedQueue.Object.FlushEvents();

            await Task.Delay(100);
            
            mockedQueue.Verify(m => m.FlushEvents(), Times.Once);
        }

        [TestMethod]
        public async Task FlushEvents_EventQueuedAndFlushed_QueueNotFlushedOnFirstAttempt_VerifyFlushEventsCalledTwice()
        {
            // Mock EventQueue for verification without mocking any methods
            var mockedQueue = new Mock<EventQueue>
            {
                CallBase = true
            };
            
            var mockResponse = new Mock<IRestResponse>();
            mockResponse.SetupGet(_ => _.StatusCode).Returns(HttpStatusCode.InternalServerError);
            
            dvcEventsApiClient.Setup(m => m.PublishEvents(It.IsAny<BatchOfUserEventsBatch>()))
                .ReturnsAsync(mockResponse.Object);
            mockedQueue.Object.SetPrivateFieldValue("dvcEventsApiClient", dvcEventsApiClient.Object);
            
            mockedQueue.Object.AddFlushedEventsSubscriber(AssertFalseFlushedEvents);

            var user = new User(userId: "1");

            var @event = new Event("testEvent", metaData: new Dictionary<string, object> {{"test", "value"}});

            var config = new BucketedUserConfig
            {
                FeatureVariationMap = new Dictionary<string, string> {{"some-feature-id", "some-variation-id"}}
            };

            var dvcPopulatedUser = new DVCPopulatedUser(user);
            mockedQueue.Object.QueueEvent(dvcPopulatedUser, @event, config);
            
            await mockedQueue.Object.FlushEvents();
            
            mockedQueue.Verify(m => m.FlushEvents(), Times.Once);
            
            // add delay so the queue should still be looping trying to flush
            await Task.Delay(100);
            mockedQueue.Verify(m => m.FlushEvents(), Times.AtLeastOnce);
            dvcEventsApiClient.Verify(m => m.PublishEvents(It.IsAny<BatchOfUserEventsBatch>()), Times.AtLeastOnce);

            // ensure the queue can now be flushed
            mockResponse.SetupGet( _ => _.StatusCode).Returns(HttpStatusCode.Created);
            
            mockedQueue.Invocations.Clear();
            dvcEventsApiClient.Invocations.Clear();
            
            mockedQueue.Object.RemoveFlushedEventsSubscriber(AssertFalseFlushedEvents);
            mockedQueue.Object.AddFlushedEventsSubscriber(AssertTrueFlushedEvents);

            // Add a longer delay to the test to ensure FlushEvents is no longer looping
            await Task.Delay(500);

            mockedQueue.Verify(m => m.FlushEvents(), Times.AtMost(2));
            dvcEventsApiClient.Verify(m => m.PublishEvents(It.IsAny<BatchOfUserEventsBatch>()), Times.AtMost(2));
            
            // internal event queue should now be empty, flush events manually and check that publish isnt called
            
            mockedQueue.Invocations.Clear();
            dvcEventsApiClient.Invocations.Clear();
            await mockedQueue.Object.FlushEvents();
            await Task.Delay(20);
            
            mockedQueue.Verify(m => m.FlushEvents(), Times.Once);
            dvcEventsApiClient.Verify(m => m.PublishEvents(It.IsAny<BatchOfUserEventsBatch>()), Times.Never());
        }
        
        [TestMethod]
        public async Task QueueTwoAggregateEvents_EventsQueuedSuccessfully()
        {
            // Mock EventQueue for verification without mocking any methods
            var mockedQueue = new Mock<EventQueue>
            {
                CallBase = true
            };
            
            var mockResponse = new Mock<IRestResponse>();
            mockResponse.SetupGet(_ => _.StatusCode).Returns(HttpStatusCode.Created);
            
            dvcEventsApiClient.Setup(m => m.PublishEvents(It.IsAny<BatchOfUserEventsBatch>()))
                .ReturnsAsync(mockResponse.Object);
            mockedQueue.Object.SetPrivateFieldValue("dvcEventsApiClient", dvcEventsApiClient.Object);

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

            var config2 = new BucketedUserConfig
            {
                FeatureVariationMap = new Dictionary<string, string> { { "feature2", "variation2" } }
            };
            
            mockedQueue.Object.QueueAggregateEvent(dvcPopulatedUser, @event, config);
            mockedQueue.Object.QueueAggregateEvent(dvcPopulatedUser, @event, config);
            mockedQueue.Object.QueueAggregateEvent(dvcPopulatedUser, @event2, config);
            mockedQueue.Object.QueueAggregateEvent(dvcPopulatedUser, @event2, config);
            mockedQueue.Object.QueueAggregateEvent(dvcPopulatedUser, @event3, config);
            mockedQueue.Object.QueueAggregateEvent(dvcPopulatedUser, @event, config2);
            mockedQueue.Object.QueueAggregateEvent(dvcPopulatedUser2, @event, config);
            mockedQueue.Object.QueueAggregateEvent(dvcPopulatedUser3, @event, config);

            mockedQueue.Object.AddFlushedEventsSubscriber(AssertTrueFlushedEvents);

            await mockedQueue.Object.FlushEvents();
            await Task.Delay(20);

            dvcEventsApiClient.Verify(m => m.PublishEvents(It.Is<BatchOfUserEventsBatch>(b =>
               AssertAggregateEventsBatch(b)
            )), Times.Once());
            
            await mockedQueue.Object.FlushEvents();
            await Task.Delay(20);
            // verify that it hasn't been called again because there should be nothing in the queue
            dvcEventsApiClient.Verify(m => m.PublishEvents(It.IsAny<BatchOfUserEventsBatch>(
            )), Times.Once());
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
            Debug.Assert(b.UserEventsBatchRecords.Count == 3);
            Debug.Assert(b.UserEventsBatchRecords[0].User.Name == "User1");
            Debug.Assert(b.UserEventsBatchRecords[0].Events.Count == 4);
            
            Debug.Assert(b.UserEventsBatchRecords[0].Events[0].Type == "variableEvaluated");
            Debug.Assert(b.UserEventsBatchRecords[0].Events[0].Target == "var1");
            Debug.Assert(b.UserEventsBatchRecords[0].Events[0].Value == 2);
            
            Debug.Assert(b.UserEventsBatchRecords[0].Events[1].Type == "variableEvaluated");
            Debug.Assert(b.UserEventsBatchRecords[0].Events[1].Target == "var2");
            Debug.Assert(b.UserEventsBatchRecords[0].Events[1].Value == 2);
            
            Debug.Assert(b.UserEventsBatchRecords[0].Events[2].Type == "variableDefaulted");
            Debug.Assert(b.UserEventsBatchRecords[0].Events[2].Target == "var2");
            Debug.Assert(b.UserEventsBatchRecords[0].Events[2].Value == 1);
            
            Debug.Assert(b.UserEventsBatchRecords[0].Events[3].Type == "variableEvaluated");
            Debug.Assert(b.UserEventsBatchRecords[0].Events[3].Target == "var1");
            Debug.Assert(b.UserEventsBatchRecords[0].Events[3].Value == 1);
            Debug.Assert(b.UserEventsBatchRecords[0].Events[3].FeatureVars["feature2"] == "variation2");

            Debug.Assert(b.UserEventsBatchRecords[1].User.Name == "User2");
            Debug.Assert(b.UserEventsBatchRecords[1].Events.Count == 1);
            Debug.Assert(b.UserEventsBatchRecords[1].Events[0].Type == "variableEvaluated");
            Debug.Assert(b.UserEventsBatchRecords[1].Events[0].Target == "var1");
            Debug.Assert(b.UserEventsBatchRecords[1].Events[0].Value == 1);

            Debug.Assert(b.UserEventsBatchRecords[2].User.Name == "User3");
            Debug.Assert(b.UserEventsBatchRecords[2].Events.Count == 1);
            Debug.Assert(b.UserEventsBatchRecords[2].Events[0].Type == "variableEvaluated");
            Debug.Assert(b.UserEventsBatchRecords[2].Events[0].Target == "var1");
            Debug.Assert(b.UserEventsBatchRecords[2].Events[0].Value == 1);

            return true;
        }
    }
}