using System;
using DevCycle.SDK.Server.Local.Api;
using DevCycle.SDK.Server.Local.ConfigManager;
using DevCycle.SDK.Server.Common.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json.Linq;

namespace DevCycle.SDK.Server.Local.MSTests
{
    // Access the internal method on DVCClientBuilder
    internal class DVCClientBuilderTest : DVCLocalClientBuilder
    {
        internal new DVCClientBuilderTest SetConfigManager(EnvironmentConfigManager environmentConfigManager)
        {
            base.SetConfigManager(environmentConfigManager);
            return this;
        }
        
        internal new DVCClientBuilderTest SetLocalBucketing(LocalBucketing localBucketing)
        {
            base.SetLocalBucketing(localBucketing);
            return this;
        }
    }


    [TestClass]
    public class DVCTest
    {
        private Mock<EnvironmentConfigManager> environmentConfigManager;
        private LocalBucketing localBucketing;
        private string environmentKey;
        private const string Config = "{\"project\":{\"_id\":\"6216420c2ea68943c8833c09\",\"key\":\"default\",\"a0_organization\":\"org_NszUFyWBFy7cr95J\"},\"environment\":{\"_id\":\"6216420c2ea68943c8833c0b\",\"key\":\"development\"},\"features\":[{\"_id\":\"6216422850294da359385e8b\",\"key\":\"test\",\"type\":\"release\",\"variations\":[{\"variables\":[{\"_var\":\"6216422850294da359385e8d\",\"value\":true}],\"name\":\"Variation On\",\"key\":\"variation-on\",\"_id\":\"6216422850294da359385e8f\"},{\"variables\":[{\"_var\":\"6216422850294da359385e8d\",\"value\":false}],\"name\":\"Variation Off\",\"key\":\"variation-off\",\"_id\":\"6216422850294da359385e90\"}],\"configuration\":{\"_id\":\"621642332ea68943c8833c4a\",\"targets\":[{\"distribution\":[{\"percentage\":0.5,\"_variation\":\"6216422850294da359385e8f\"},{\"percentage\":0.5,\"_variation\":\"6216422850294da359385e90\"}],\"_audience\":{\"_id\":\"621642332ea68943c8833c4b\",\"filters\":{\"operator\":\"and\",\"filters\":[{\"values\":[],\"type\":\"all\",\"filters\":[]}]}},\"_id\":\"621642332ea68943c8833c4d\"}],\"forcedUsers\":{}}}],\"variables\":[{\"_id\":\"6216422850294da359385e8d\",\"key\":\"test\",\"type\":\"Boolean\"}],\"variableHashes\":{\"test\":2447239932}}";

        [TestInitialize]
        public void BeforeEachTest()
        {
            localBucketing = new LocalBucketing();
            environmentConfigManager = new Mock<EnvironmentConfigManager>();
            var initializedEventArgs = new DVCEventArgs
            {
                Success = true
            };
            environmentConfigManager.Setup(m => m.InitializeConfigAsync()).ReturnsAsync(initializedEventArgs);
            environmentConfigManager.Object.SetPrivateFieldValue("localBucketing", localBucketing);
            
            environmentKey = $"server-{Guid.NewGuid()}";
            environmentConfigManager.SetupGet(m => m.Config).Returns(Config);
            environmentConfigManager.SetupGet(m => m.Initialized).Returns(true);
            
            localBucketing.StoreConfig(environmentKey, Config);
        }
        
        [TestMethod]
        public void GetFeaturesTest()
        {
            DVCClientBuilderTest apiBuilder = new DVCClientBuilderTest();
            using DVCLocalClient api = (DVCLocalClient) apiBuilder
                .SetConfigManager(environmentConfigManager.Object)
                .SetLocalBucketing(localBucketing)
                .SetEnvironmentKey(environmentKey)
                .Build();

            var user = new User("j_test");
            var result = api.AllFeatures(user);
            
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.IsNotNull(result["test"]);
        }

        [TestMethod]
        public void GetVariableByKeyTest()
        {
            DVCClientBuilderTest apiBuilder = new DVCClientBuilderTest();
            using DVCLocalClient api = (DVCLocalClient) apiBuilder
                .SetConfigManager(environmentConfigManager.Object)
                .SetLocalBucketing(localBucketing)
                .SetEnvironmentKey(environmentKey)
                .Build();

            var user = new User("j_test");
            string key = "test";
            var result = api.Variable(user, key, false);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Value);
        }
        
        [TestMethod]
        public void GetJsonVariableByKeyReturnsDefaultTest()
        {
            DVCClientBuilderTest apiBuilder = new DVCClientBuilderTest();
            using DVCLocalClient api = (DVCLocalClient) apiBuilder
                .SetConfigManager(environmentConfigManager.Object)
                .SetLocalBucketing(localBucketing)
                .SetEnvironmentKey(environmentKey)
                .Build();

            var user = new User("j_test");
            string key = "json";
            
            string json = "['Small','Medium','Large']";
            var expectedValue = JArray.Parse(json);

            var result = api.Variable(user, key, JArray.Parse(json));

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefaulted);
            Assert.AreEqual(expectedValue.ToString(), result.Value.ToString());
        }

        [TestMethod]
        public void GetVariablesTest()
        {
            DVCClientBuilderTest apiBuilder = new DVCClientBuilderTest();
            using DVCLocalClient api = (DVCLocalClient) apiBuilder
                .SetConfigManager(environmentConfigManager.Object)
                .SetLocalBucketing(localBucketing)
                .SetEnvironmentKey(environmentKey)
                .Build();

            User user = new User("j_test");

            var result = api.AllVariables(user);

            Assert.IsNotNull(result);

            var variable = result.Get<bool>("test");
            Assert.IsNotNull(variable);
            Assert.IsTrue(variable.Value);
        }

        [TestMethod]
        public void PostEventsTest()
        {
            DVCClientBuilderTest apiBuilder = new DVCClientBuilderTest();
            using DVCLocalClient api = (DVCLocalClient) apiBuilder
                .SetConfigManager(environmentConfigManager.Object)
                .SetLocalBucketing(localBucketing)
                .SetEnvironmentKey(environmentKey)
                .Build();

            DateTimeOffset now = DateTimeOffset.UtcNow;
            long unixTimeMilliseconds = now.ToUnixTimeMilliseconds();

            User user = new User("j_test");
            Event userEvent = new Event("test event", "test target", unixTimeMilliseconds, 600);

            api.Track(user, userEvent);
        }

        private void MyCallback(object sender, DVCEventArgs e)
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void Variable_NullUser_ThrowsException()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                DVCClientBuilderTest apiBuilder = new DVCClientBuilderTest();
                using DVCLocalClient api = (DVCLocalClient) apiBuilder
                    .SetConfigManager(environmentConfigManager.Object)
                    .SetEnvironmentKey("INSERT_SDK_KEY")
                    .Build();
                
                api.Variable(null, "some_key", true);
            });
        }

        [TestMethod]
        public void User_NullUserId_ThrowsException()
        {
            Assert.ThrowsException<ArgumentException>(() =>
            {
                _ = new User();
            });
        }
    }
}