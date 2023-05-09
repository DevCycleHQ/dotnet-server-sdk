using System;
using System.Net;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Local.Api;
using DevCycle.SDK.Server.Local.ConfigManager;
using DevCycle.SDK.Server.Common.API;
using DevCycle.SDK.Server.Common.Model;
using DevCycle.SDK.Server.Common.Model.Local;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Newtonsoft.Json.Linq;
using RestSharp;
using RichardSzalay.MockHttp;
using Environment = System.Environment;
using System.Collections.Generic;

namespace DevCycle.SDK.Server.Local.MSTests
{
    [TestClass]
    public class DVCTest
    {
        private DVCLocalClient getTestClient(DVCLocalOptions options = null, string config = null,
            bool skipInitialize = false)
        {
            if (config == null)
            {
                config = new string(Fixtures.Config());
            }

            var mockHttp = new MockHttpMessageHandler();

            mockHttp.When("https://config-cdn*")
                .Respond(HttpStatusCode.OK, "application/json",
                    config);
            mockHttp.When("https://events*")
                .Respond(HttpStatusCode.Created, "application/json",
                    "{}");
            var localBucketing = new LocalBucketing();
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var sdkKey = $"dvc_server_{Guid.NewGuid().ToString().Replace('-', '_')}_hash";
            localBucketing.StoreConfig(sdkKey, config);
            var configManager = new EnvironmentConfigManager(sdkKey, options ?? new DVCLocalOptions(),
                new NullLoggerFactory(),
                localBucketing,
                restClientOptions: new DVCRestClientOptions() { ConfigureMessageHandler = _ => mockHttp });
            if (skipInitialize)
            {
                configManager.Initialized = false;
            }
            else
            {
                configManager.Initialized = true;
            }

            DVCLocalClient api = new DVCLocalClientBuilder()
                .SetLocalBucketing(localBucketing)
                .SetConfigManager(configManager)
                .SetRestClientOptions(new DVCRestClientOptions() { ConfigureMessageHandler = _ => mockHttp })
                .SetOptions(options ?? new DVCLocalOptions())
                .SetSDKKey(sdkKey)
                .SetLogger(loggerFactory)
                .Build();
            return api;
        }

        [TestMethod]
        public async Task CustomCDNURITest()
        {
            const string baseurl = "https://different-domain";
            const string slug = "/slug";
            var testClient = new DVCLocalClientBuilder()
                .SetOptions(new DVCLocalOptions()
                {
                    CdnUri = baseurl,
                    CdnSlug = slug
                })
                .SetInitializedSubscriber((_, args) =>
                {
                    Assert.IsTrue(args.Errors.Count != 0);
                    Console.WriteLine("Failed to get config because: " + args.Errors[0].ErrorResponse);
                })
                .SetSDKKey("dvc_server_CustomCDNURITest")
                .Build();
            await Task.Delay(5000);
        }

        [TestMethod]
        public async Task GetProductionAllVariables()
        {
            var sdkKey = Environment.GetEnvironmentVariable("DVC_SERVER_SDK_KEY");
            if (string.IsNullOrEmpty(sdkKey))
            {
                Console.WriteLine(
                    "DVC_SERVER_SDK_KEY is not set in the environment variables - skipping production features test.");
                return;
            }

            var api = new DVCLocalClientBuilder()
                .SetInitializedSubscriber(((sender, args) => { Console.WriteLine($"Success? : {args.Success}"); }))
                .SetSDKKey(sdkKey)
                .Build();

            await Task.Delay(5000);
            var resp = api.AllFeatures(new User("test"));
            Assert.IsTrue(resp.Count > 0);
            foreach (var (key, value) in resp)
            {
                Console.WriteLine(key, value);
            }
        }

        [TestMethod]
        public void GetFeaturesTest()
        {
            var api = getTestClient();
            var user = new User("j_test");
            user.Country = "CA";
            user.Language = "en";
            user.AppBuild = 1;
            user.Email = "email@gmail.com";
            user.Name = "name";
            var result = api.AllFeatures(user);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.IsNotNull(result["test"]);
            Assert.IsFalse(string.IsNullOrEmpty(result["test"].VariationKey));
            Assert.IsFalse(string.IsNullOrEmpty(result["test"].VariationName));
        }

        [TestMethod]
        public async Task GetVariableByKeyTestAsync()
        {
            using DVCLocalClient api = getTestClient();

            var user = new User("j_test");
            string key = "test";
            await Task.Delay(3000);
            
            var variable = api.Variable(user, key, false);
            Assert.IsNotNull(variable);
            Assert.IsTrue(variable.Value);

            var value = api.VariableValue(user, key, false);
            Assert.IsTrue(value);
        }
        
        [TestMethod]
        public async Task GetVariableByKeySpecialCharactersTestAsync()
        {
            using DVCLocalClient api = getTestClient(config: Fixtures.ConfigWithSpecialCharacters());
            var user = new User("j_test");
            string key = Fixtures.VariableKey;
            await Task.Delay(3000);
            
            var variable = api.Variable<string>(user, key, "default_value");
            Assert.IsNotNull(variable);
            Assert.IsNotNull(variable.Value);
            Assert.AreEqual("öé 🐍 ¥", variable.Value);
            
            var value = api.VariableValue(user, key, "default_value");
            Assert.IsNotNull(value);
            Assert.AreEqual("öé 🐍 ¥", value);
        }
        
        [TestMethod]
        public async Task GetVariableByKeyJsonObjTestAsync()
        {
            using DVCLocalClient api = getTestClient(config: Fixtures.ConfigWithJSONValues());
            var user = new User("j_test");
            string key = Fixtures.VariableKey;
            await Task.Delay(3000);
            
            var expectedValue = JObject.Parse("{\"sample\": \"A\"}");
            var defaultValue = JObject.Parse("{\"key\": \"default\"}");
            var variable = api.Variable<JObject>(user, key, defaultValue);
            Assert.IsNotNull(variable);
            Assert.IsNotNull(variable.Value);
            Assert.AreEqual(expectedValue.ToString(), variable.Value.ToString());

            var value = api.VariableValue(user, key, defaultValue);
            Assert.IsNotNull(value);
            Assert.AreEqual(expectedValue.ToString(), value.ToString());
        }

        
        [TestMethod]
        public void GetJsonVariableByKeyReturnsDefaultArrayTest()
        {
            using DVCLocalClient api = getTestClient();

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
        public void GetJsonVariableByKeyReturnsDefaultObjectTest()
        {
            using DVCLocalClient api = getTestClient();

            var user = new User("j_test");
            string key = "json";

            string json = "{\"key\": \"value\"}";
            var expectedValue = JObject.Parse(json);
            
            var variable = api.Variable(user, key, JObject.Parse(json));
            Assert.IsNotNull(variable);
            Assert.IsTrue(variable.IsDefaulted);
            Assert.AreEqual(expectedValue.ToString(), variable.Value.ToString());

            var value = api.VariableValue(user, key, JObject.Parse(json));
            Assert.IsNotNull(value);
            Assert.AreEqual(expectedValue.ToString(), value.ToString());
        }

        [TestMethod]
        public async Task GetVariablesTest()
        {
            using DVCLocalClient api = getTestClient();

            User user = new User("j_test");
            // Bucketing needs time to work.
            await Task.Delay(5000);
            var result = api.AllVariables(user);

            Assert.IsTrue(result.ContainsKey("test"));
            Assert.IsNotNull(result);
            var variable = result["test"];
            Assert.IsNotNull(variable);
            Assert.IsTrue((Boolean)variable.Value);
        }

        [TestMethod]
        public async Task PostEventsTest()
        {
            using DVCLocalClient api = getTestClient();


            DateTimeOffset now = DateTimeOffset.UtcNow;

            User user = new User("j_test");
            Event userEvent = new Event("test event", "test target", now.DateTime, 600);
            await Task.Delay(5000);
            api.Track(user, userEvent);

        }

        [TestMethod]
        public void Variable_NullUser_ThrowsException()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                using DVCLocalClient api = getTestClient();

                api.Variable(null, "some_key", true);
            });
        }

        [TestMethod]
        public void User_NullUserId_ThrowsException()
        {
            Assert.ThrowsException<ArgumentException>(() => { _ = new User(); });
        }

        [TestMethod]
        public void User_InvalidUserIdLength_ThrowsException()
        {
            Assert.ThrowsException<ArgumentException>(() =>
            {
                _ = new User("Oy0mkUHONE6Qg36DhrOrwbvkCaxiMQPClHsELgFdfdlYCcE0AGyJqgl2tnV6Ago2"
                             + "7uUXlXvChzLiLHPGRDavA9H82lM47B1pFOW51KQhT9kxLU1PgLfs2NOlekOWldtT9jh"
                             + "JdgsDl0Cm49Vb7utlc4y0dyHYS1GKFuJwuipzVSrlYij39D8BWKLDbkqiJGc7qU2xCAeJv");
            });
        }
        
        [TestMethod]
        public void SetClientCustomDataTest()
        {
            using DVCLocalClient api = getTestClient();
            
            Dictionary<string, object> customData = new Dictionary<string, object>();
            customData.Add("strProp", "value");
            customData.Add("numProp", 100);
            customData.Add("boolProp", true);
            customData.Add("nullProp", null);
            
            api.SetClientCustomData(customData);
        }
    }
}  
