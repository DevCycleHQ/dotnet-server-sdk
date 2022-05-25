using System.Collections.Generic;
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

            // ensure the queue can now be flushed
            mockedQueue.Invocations.Clear();
            mockResponse.SetupGet( _ => _.StatusCode).Returns(HttpStatusCode.Created);
            
            mockedQueue.Object.RemoveFlushedEventsSubscriber(AssertFalseFlushedEvents);
            mockedQueue.Object.AddFlushedEventsSubscriber(AssertTrueFlushedEvents);

            // Add a longer delay to the test to ensure FlushEvents is no longer looping
            await Task.Delay(500);

            mockedQueue.Verify(m => m.FlushEvents(), Times.AtMost(2));
        }
        
        [TestMethod]
        public void QueueTwoAggregateEvents_EventsQueuedSuccessfully()
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

            var user = new User(userId: "1");

            var @event = new Event("VariableEvaluated", "config1");

            var config = new BucketedUserConfig
            {
                FeatureVariationMap = new Dictionary<string, string> {{"some-feature-id", "some-variation-id"}}
            };

            var dvcPopulatedUser = new DVCPopulatedUser(user);
            mockedQueue.Object.QueueAggregateEvent(dvcPopulatedUser, @event, config);
            
            @event = new Event("VariableEvaluated", "config2");
            mockedQueue.Object.QueueAggregateEvent(dvcPopulatedUser, @event, config);
        }
        
        private void AssertTrueFlushedEvents(object sender, DVCEventArgs e)
        {
            Assert.IsTrue(e.Success);
        }
        
        private void AssertFalseFlushedEvents(object sender, DVCEventArgs e)
        {
            Assert.IsFalse(e.Success);
        }
    }
}