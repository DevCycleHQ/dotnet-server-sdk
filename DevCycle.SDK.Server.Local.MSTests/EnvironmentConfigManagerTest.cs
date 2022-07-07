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
using Microsoft.VisualBasic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RestSharp;

namespace DevCycle.SDK.Server.Local.MSTests
{
    [TestClass]
    public class EnvironmentConfigManagerTest
    {
        private Mock<RestClient> mockRestClient;
        private ILoggerFactory loggerFactory;
        private DVCLocalOptions localOptions;
        private LocalBucketing localBucketing;

        [TestInitialize]
        public void BeforeEachTest()
        {
            mockRestClient = new Mock<RestClient>();
            loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            localOptions = new DVCLocalOptions();
            localBucketing = new LocalBucketing();
        }
        
        private void SetupRestClient (bool shouldThrow = false, bool shouldError = false, HttpStatusCode errorStatus = HttpStatusCode.InternalServerError)
        {
            const string config = "{\"project\":{\"_id\":\"6216420c2ea68943c8833c09\",\"key\":\"default\",\"a0_organization\":\"org_NszUFyWBFy7cr95J\"},\"environment\":{\"_id\":\"6216420c2ea68943c8833c0b\",\"key\":\"development\"},\"features\":[{\"_id\":\"6216422850294da359385e8b\",\"key\":\"test\",\"type\":\"release\",\"variations\":[{\"variables\":[{\"_var\":\"6216422850294da359385e8d\",\"value\":true}],\"name\":\"Variation On\",\"key\":\"variation-on\",\"_id\":\"6216422850294da359385e8f\"},{\"variables\":[{\"_var\":\"6216422850294da359385e8d\",\"value\":false}],\"name\":\"Variation Off\",\"key\":\"variation-off\",\"_id\":\"6216422850294da359385e90\"}],\"configuration\":{\"_id\":\"621642332ea68943c8833c4a\",\"targets\":[{\"distribution\":[{\"percentage\":0.5,\"_variation\":\"6216422850294da359385e8f\"},{\"percentage\":0.5,\"_variation\":\"6216422850294da359385e90\"}],\"_audience\":{\"_id\":\"621642332ea68943c8833c4b\",\"filters\":{\"operator\":\"and\",\"filters\":[{\"values\":[],\"type\":\"all\",\"filters\":[]}]}},\"_id\":\"621642332ea68943c8833c4d\"}],\"forcedUsers\":{}}}],\"variables\":[{\"_id\":\"6216422850294da359385e8d\",\"key\":\"test\",\"type\":\"Boolean\"}],\"variableHashes\":{\"test\":2447239932}}";
            // mock headers
            var headers = new Mock<HttpHeaders>();
            var headersList = new System.Collections.Generic.List<string> {"test etag"};
            headers.Setup(_ => _.GetValues("etag")).Returns(headersList);

            if (shouldThrow)
            {
                mockRestClient.Setup(_ => _.ExecuteAsync(It.IsAny<RestRequest>(), It.IsAny<System.Threading.CancellationToken>()))
                    .Throws(new DVCException(errorStatus, new ErrorResponse("test exception")));
            }
            else
            {
                SetupSuccessForRestClient(shouldError, errorStatus);
            }
            
        }

        private void SetupSuccessForRestClient(bool shouldError = false, HttpStatusCode errorStatus = HttpStatusCode.InternalServerError)
        {
            const string config = "{\"project\":{\"_id\":\"6216420c2ea68943c8833c09\",\"key\":\"default\",\"a0_organization\":\"org_NszUFyWBFy7cr95J\"},\"environment\":{\"_id\":\"6216420c2ea68943c8833c0b\",\"key\":\"development\"},\"features\":[{\"_id\":\"6216422850294da359385e8b\",\"key\":\"test\",\"type\":\"release\",\"variations\":[{\"variables\":[{\"_var\":\"6216422850294da359385e8d\",\"value\":true}],\"name\":\"Variation On\",\"key\":\"variation-on\",\"_id\":\"6216422850294da359385e8f\"},{\"variables\":[{\"_var\":\"6216422850294da359385e8d\",\"value\":false}],\"name\":\"Variation Off\",\"key\":\"variation-off\",\"_id\":\"6216422850294da359385e90\"}],\"configuration\":{\"_id\":\"621642332ea68943c8833c4a\",\"targets\":[{\"distribution\":[{\"percentage\":0.5,\"_variation\":\"6216422850294da359385e8f\"},{\"percentage\":0.5,\"_variation\":\"6216422850294da359385e90\"}],\"_audience\":{\"_id\":\"621642332ea68943c8833c4b\",\"filters\":{\"operator\":\"and\",\"filters\":[{\"values\":[],\"type\":\"all\",\"filters\":[]}]}},\"_id\":\"621642332ea68943c8833c4d\"}],\"forcedUsers\":{}}}],\"variables\":[{\"_id\":\"6216422850294da359385e8d\",\"key\":\"test\",\"type\":\"Boolean\"}],\"variableHashes\":{\"test\":2447239932}}";
            // mock headers
            var headers = new Mock<HttpHeaders>();
            var headersList = new System.Collections.Generic.List<string> { "test etag" };
            var headersResp = new Collection<HeaderParameter>(){new ("test etag", null)};
            headers.Setup(_ => _.GetValues("etag")).Returns(headersList);
            var response = new Mock<RestResponse>();
            response.Setup(_ => _.StatusCode).Returns(shouldError ? errorStatus : HttpStatusCode.OK);
            response.Setup(_ => _.IsSuccessful).Returns(!shouldError);
            response.Setup(_ => _.Content).Returns(config);
            // This test needs to be fixed.
            response.Setup(_ => _.ContentHeaders).Returns(headersResp);
            mockRestClient.Setup(_ => _.ExecuteAsync(It.IsAny<RestRequest>(), It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(response.Object);
        }

        [TestMethod]
        public async Task PollForConfigTest()
        {
            var configManager = new EnvironmentConfigManager("server-key", new DVCLocalOptions(), loggerFactory, localBucketing);
            SetupRestClient();
            configManager.SetPrivateFieldValue("restClient", mockRestClient.Object);
            await configManager.InitializeConfigAsync();
            await Task.Delay(2000);
            mockRestClient.Verify(v => v.ExecuteAsync(It.IsAny<RestRequest>(), It.IsAny<System.Threading.CancellationToken>()), Times.Exactly(3));
        }
        
        [TestMethod]
        public async Task PollForConfigNonRetryableTest()
        {
            var configManager = new EnvironmentConfigManager("server-key", new DVCLocalOptions(), loggerFactory, localBucketing, DidNotInitializeSubscriber);
            SetupRestClient(shouldError: true, errorStatus: HttpStatusCode.BadRequest);
            configManager.SetPrivateFieldValue("restClient", mockRestClient.Object);
            await configManager.InitializeConfigAsync();
            await Task.Delay(2000);
            // expect only one request since it failed in a non-retryable way
            mockRestClient.Verify(v => v.ExecuteAsync(It.IsAny<RestRequest>(), It.IsAny<System.Threading.CancellationToken>()), Times.Exactly(1));
        }
        
        
        [TestMethod]
        public async Task PollForConfigRetryableTest()
        {
            var configManager = new EnvironmentConfigManager("server-key", new DVCLocalOptions(), loggerFactory, localBucketing, DidNotInitializeSubscriber);
            SetupRestClient(shouldError: true, errorStatus: HttpStatusCode.InternalServerError);
            configManager.SetPrivateFieldValue("restClient", mockRestClient.Object);
            await configManager.InitializeConfigAsync();
            await Task.Delay(2000);
            // expect several retry requests
            mockRestClient.Verify(v => v.ExecuteAsync(It.IsAny<RestRequest>(), It.IsAny<System.Threading.CancellationToken>()), Times.Exactly(3));
        }

        [TestMethod]
        public async Task OnSuccessNotificationTest()
        {
            var configManager = new EnvironmentConfigManager("server-key", new DVCLocalOptions(), loggerFactory,
                localBucketing, DidInitializeSubscriber);
            SetupRestClient();
            configManager.SetPrivateFieldValue("restClient", mockRestClient.Object);
            await configManager.InitializeConfigAsync();
        }

        [TestMethod]
        public async Task OnErrorNotificationTest()
        {
            var configManager = new EnvironmentConfigManager("server-key", localOptions, loggerFactory,
                localBucketing, DidNotInitializeSubscriber);
            SetupRestClient(shouldError: true);
            configManager.SetPrivateFieldValue("restClient", mockRestClient.Object);
            await configManager.InitializeConfigAsync();
        }

        [TestMethod]
        public async Task OnExceptionNotificationTest()
        {
            var configManager = new EnvironmentConfigManager("server-key", localOptions, loggerFactory,
                localBucketing, DidNotInitializeSubscriber);
            SetupRestClient(shouldThrow: true);
            configManager.SetPrivateFieldValue("restClient", mockRestClient.Object);
            await configManager.InitializeConfigAsync();
        }
        
        [TestMethod]
        public async Task initializeConfigAsync_configIsNotFetched_thenFetchedOnNextCall_NotificationIsSuccessful()
        {
            var configCallCount = 0;
            var configManager = new EnvironmentConfigManager("server-key", localOptions, loggerFactory, localBucketing,
                ((o, e) =>
                {
                    configCallCount++;

                    if (configCallCount == 1)
                    {
                        Assert.IsFalse(e.Success);
                        SetupSuccessForRestClient();
                    }
                    else
                    {
                        Assert.IsTrue(e.Success);
                    }
                }));
            SetupRestClient(shouldThrow: true);
            configManager.SetPrivateFieldValue("restClient", mockRestClient.Object);
            await configManager.InitializeConfigAsync();
            await Task.Delay(2000);
            
            Assert.AreEqual(2, configCallCount);
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