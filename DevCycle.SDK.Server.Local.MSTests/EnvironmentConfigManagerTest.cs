using System.Threading.Tasks;

using Moq;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using DevCycle.ConfigManager;
using RestSharp.Portable;
using System.Net;
using DevCycle.Api;
using DevCycle.SDK.Server.Common.Exception;
using DevCycle.SDK.Server.Common.Model;
using DevCycle.SDK.Server.Common.Model.Local;
using Microsoft.Extensions.Logging;

namespace DevCycle.MSTests
{
    [TestClass]
    public class EnvironmentConfigManagerTest
    {
        private Mock<IRestClient> mockRestClient;
        private ILoggerFactory loggerFactory;
        private DVCOptions options;
        private LocalBucketing localBucketing;

        [TestInitialize]
        public void BeforeEachTest()
        {
            mockRestClient = new Mock<IRestClient>();
            loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            options = new DVCOptions();
            localBucketing = new LocalBucketing();
        }
        
        private void SetupRestClient (bool shouldThrow = false, bool shouldError = false)
        {
            var config = "{\"project\":{\"_id\":\"6216420c2ea68943c8833c09\",\"key\":\"default\",\"a0_organization\":\"org_NszUFyWBFy7cr95J\"},\"environment\":{\"_id\":\"6216420c2ea68943c8833c0b\",\"key\":\"development\"},\"features\":[{\"_id\":\"6216422850294da359385e8b\",\"key\":\"test\",\"type\":\"release\",\"variations\":[{\"variables\":[{\"_var\":\"6216422850294da359385e8d\",\"value\":true}],\"name\":\"Variation On\",\"key\":\"variation-on\",\"_id\":\"6216422850294da359385e8f\"},{\"variables\":[{\"_var\":\"6216422850294da359385e8d\",\"value\":false}],\"name\":\"Variation Off\",\"key\":\"variation-off\",\"_id\":\"6216422850294da359385e90\"}],\"configuration\":{\"_id\":\"621642332ea68943c8833c4a\",\"targets\":[{\"distribution\":[{\"percentage\":0.5,\"_variation\":\"6216422850294da359385e8f\"},{\"percentage\":0.5,\"_variation\":\"6216422850294da359385e90\"}],\"_audience\":{\"_id\":\"621642332ea68943c8833c4b\",\"filters\":{\"operator\":\"and\",\"filters\":[{\"values\":[],\"type\":\"all\",\"filters\":[]}]}},\"_id\":\"621642332ea68943c8833c4d\"}],\"forcedUsers\":{}}}],\"variables\":[{\"_id\":\"6216422850294da359385e8d\",\"key\":\"test\",\"type\":\"Boolean\"}],\"variableHashes\":{\"test\":2447239932}}";
            // mock headers
            var headers = new Mock<IHttpHeaders>();
            var headersList = new System.Collections.Generic.List<string> {"test etag"};
            headers.Setup(_ => _.GetValues("etag")).Returns(headersList);

            //mock response
            var response = new Mock<IRestResponse>();
            if (!shouldThrow)
            {
                response.Setup(_ => _.StatusCode).Returns(shouldError ? HttpStatusCode.BadRequest : HttpStatusCode.OK);
                response.Setup(_ => _.IsSuccess).Returns(!shouldError);
                response.Setup(_ => _.Content).Returns(config);
                response.Setup(_ => _.Headers).Returns(headers.Object);
                mockRestClient.Setup(_ => _.Execute(It.IsAny<RestRequest>(), It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(response.Object);
            }
            else
            {
                mockRestClient.Setup(_ => _.Execute(It.IsAny<RestRequest>(), It.IsAny<System.Threading.CancellationToken>())).Throws(new DVCException(HttpStatusCode.BadRequest, new ErrorResponse("test exception")));
            }
        }

        [TestMethod]
        public async Task PollForConfigTest()
        {
            var configManager = new EnvironmentConfigManager("server-key", new DVCOptions(), loggerFactory, localBucketing);
            SetupRestClient();
            configManager.SetPrivateFieldValue("restClient", mockRestClient.Object);
            await configManager.InitializeConfigAsync();
            await Task.Delay(2000);
            mockRestClient.Verify(v => v.Execute(It.IsAny<RestRequest>(), It.IsAny<System.Threading.CancellationToken>()), Times.Exactly(3));
        }

        [TestMethod]
        public async Task OnSuccessCallbackTest()
        {
            var configManager = new EnvironmentConfigManager("server-key", new DVCOptions(), loggerFactory, localBucketing);
            SetupRestClient();
            configManager.SetPrivateFieldValue("restClient", mockRestClient.Object);
            var initializedEventArgs = await configManager.InitializeConfigAsync();

            Assert.IsTrue(initializedEventArgs.Success);
        }

        [TestMethod]
        public async Task OnErrorCallbackTest()
        {
            var configManager = new EnvironmentConfigManager("server-key", options, loggerFactory, localBucketing);
            SetupRestClient(shouldError: true);
            configManager.SetPrivateFieldValue("restClient", mockRestClient.Object);
            var initializedEventArgs = await configManager.InitializeConfigAsync();

            Assert.IsFalse(initializedEventArgs.Success);
            Assert.IsNotNull(initializedEventArgs.Error);
        }

        [TestMethod]
        public async Task OnExceptionCallbackTest()
        {
            var configManager = new EnvironmentConfigManager("server-key", options, loggerFactory, localBucketing);
            SetupRestClient(shouldThrow: true);
            configManager.SetPrivateFieldValue("restClient", mockRestClient.Object);
            var initializedEventArgs = await configManager.InitializeConfigAsync();

            Assert.IsFalse(initializedEventArgs.Success);
            Assert.IsNotNull(initializedEventArgs.Error);
        }
    }
}