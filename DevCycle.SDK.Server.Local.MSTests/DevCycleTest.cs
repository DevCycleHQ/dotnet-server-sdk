﻿using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Local.Api;
using DevCycle.SDK.Server.Local.ConfigManager;
using DevCycle.SDK.Server.Common.API;
using DevCycle.SDK.Server.Common.Model;
using DevCycle.SDK.Server.Common.Model.Local;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RichardSzalay.MockHttp;
using Environment = System.Environment;
using System.Collections.Generic;
using System.Text.Json;
using DevCycle.SDK.Server.Cloud.MSTests;
using DevCycle.SDK.Server.Common;
using OpenFeature;
using OpenFeature.Constant;
using OpenFeature.Model;

namespace DevCycle.SDK.Server.Local.MSTests
{
    [TestClass]
    public class DevCycleTest
    {
        private DevCycleLocalClient getTestClient(DevCycleLocalOptions options = null, string config = null,
            bool skipInitialize = false)
        {
            config ??= new string(Fixtures.Config());

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
            var configManager = new EnvironmentConfigManager(sdkKey, options ?? new DevCycleLocalOptions(),
                new NullLoggerFactory(),
                localBucketing,
                restClientOptions: new DevCycleRestClientOptions() { ConfigureMessageHandler = _ => mockHttp });
            configManager.Initialized = !skipInitialize;

            DevCycleLocalClient api = new DevCycleLocalClientBuilder()
                .SetLocalBucketing(localBucketing)
                .SetConfigManager(configManager)
                .SetRestClientOptions(new DevCycleRestClientOptions() { ConfigureMessageHandler = _ => mockHttp })
                .SetOptions(options ?? new DevCycleLocalOptions())
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
            var testClient = new DevCycleLocalClientBuilder()
                .SetOptions(new DevCycleLocalOptions()
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
            var sdkKey = Environment.GetEnvironmentVariable("DEVCYCLE_SERVER_SDK_KEY");
            if (string.IsNullOrEmpty(sdkKey))
            {
                Console.WriteLine(
                    "DEVCYCLE_SERVER_SDK_KEY is not set in the environment variables - skipping production features test.");
                return;
            }

            var api = new DevCycleLocalClientBuilder()
                .SetInitializedSubscriber(((sender, args) => { Console.WriteLine($"Success? : {args.Success}"); }))
                .SetSDKKey(sdkKey)
                .Build();

            await Task.Delay(5000);
            var resp = await api.AllFeatures(new DevCycleUser("test"));
            Assert.IsTrue(resp.Count > 0);
            foreach (var (key, value) in resp)
            {
                Console.WriteLine(key, value);
            }
        }

        [TestMethod]
        public async Task GetFeaturesTest()
        {
            var api = getTestClient();
            var user = new DevCycleUser("j_test");
            user.Country = "CA";
            user.Language = "en";
            user.AppBuild = 1;
            user.Email = "email@gmail.com";
            user.Name = "name";
            var result = await api.AllFeatures(user);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.IsNotNull(result["test"]);
            Assert.IsFalse(string.IsNullOrEmpty(result["test"].VariationKey));
            Assert.IsFalse(string.IsNullOrEmpty(result["test"].VariationName));
        }

        [TestMethod]
        public async Task GetVariableByKeyTestAsync()
        {
            using DevCycleLocalClient api = getTestClient();

            var user = new DevCycleUser("j_test");
            string key = "test";
            await Task.Delay(3000);

            var variable = await api.Variable(user, key, false);
            Assert.IsNotNull(variable);
            Assert.IsTrue(variable.Value);

            var value = await api.VariableValue(user, key, false);
            Assert.IsTrue(value);
        }

        [TestMethod]
        public async Task GetVariableByKeySpecialCharactersTestAsync()
        {
            using DevCycleLocalClient api = getTestClient(config: Fixtures.ConfigWithSpecialCharacters());
            var user = new DevCycleUser("j_test");
            string key = Fixtures.VariableKey;
            await Task.Delay(3000);

            var variable = await api.Variable(user, key, "default_value");
            Assert.IsNotNull(variable);
            Assert.IsNotNull(variable.Value);
            Assert.AreEqual("öé 🐍 ¥", variable.Value);

            var value = await api.VariableValue(user, key, "default_value");
            Assert.IsNotNull(value);
            Assert.AreEqual("öé 🐍 ¥", value);
        }

        [TestMethod]
        public async Task GetVariableByKeyJsonObjTestAsync()
        {
            using DevCycleLocalClient api = getTestClient(config: Fixtures.ConfigWithJSONValues());
            var user = new DevCycleUser("j_test");
            string key = Fixtures.VariableKey;
            await Task.Delay(3000);

            var expectedValue = Newtonsoft.Json.Linq.JObject.Parse("{\"sample\": \"A\"}");
            var defaultValue = Newtonsoft.Json.Linq.JObject.Parse("{\"key\": \"default\"}");
            var variable = await api.Variable(user, key, defaultValue);
            Assert.IsNotNull(variable);
            Assert.IsNotNull(variable.Value);
            Assert.AreEqual(expectedValue.ToString(), variable.Value.ToString());

            var value = await api.VariableValue(user, key, defaultValue);
            Assert.IsNotNull(value);
            Assert.AreEqual(expectedValue.ToString(), value.ToString());
        }


        [TestMethod]
        public async Task GetJsonVariableByKeyReturnsDefaultArrayTest()
        {
            using DevCycleLocalClient api = getTestClient();

            var user = new DevCycleUser("j_test");
            string key = "json";

            string json = "['Small','Medium','Large']";
            var expectedValue = Newtonsoft.Json.Linq.JArray.Parse(json);

            var result = await api.Variable(user, key, Newtonsoft.Json.Linq.JArray.Parse(json));

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefaulted);
            Assert.AreEqual(expectedValue.ToString(), result.Value.ToString());
        }

        [TestMethod]
        public async Task GetJsonVariableByKeyReturnsDefaultObjectTest()
        {
            using DevCycleLocalClient api = getTestClient();

            var user = new DevCycleUser("j_test");
            string key = "json";

            string json = "{\"key\": \"value\"}";
            var expectedValue = Newtonsoft.Json.Linq.JObject.Parse(json);

            var variable = await api.Variable(user, key, Newtonsoft.Json.Linq.JObject.Parse(json));
            Assert.IsNotNull(variable);
            Assert.IsTrue(variable.IsDefaulted);
            Assert.AreEqual(expectedValue.ToString(), variable.Value.ToString());

            var value = await api.VariableValue(user, key, Newtonsoft.Json.Linq.JObject.Parse(json));
            Assert.IsNotNull(value);
            Assert.AreEqual(expectedValue.ToString(), value.ToString());
        }

        [TestMethod]
        public async Task GetVariablesTest()
        {
            using DevCycleLocalClient api = getTestClient();

            DevCycleUser user = new DevCycleUser("j_test");
            // Bucketing needs time to work.
            await Task.Delay(5000);
            var result = await api.AllVariables(user);

            Assert.IsTrue(result.ContainsKey("test"));
            Assert.IsNotNull(result);
            var variable = result["test"];
            Assert.IsNotNull(variable);
            Assert.IsTrue((Boolean)variable.Value);
        }

        [TestMethod]
        public async Task PostEventsTest()
        {
            using DevCycleLocalClient api = getTestClient();


            DateTimeOffset now = DateTimeOffset.UtcNow;

            DevCycleUser user = new DevCycleUser("j_test");
            DevCycleEvent userEvent = new DevCycleEvent("test event", "test target", now.DateTime, 600);
            await Task.Delay(5000);
            await api.Track(user, userEvent);
        }

        [TestMethod]
        public void Variable_NullUser_ThrowsException()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                using DevCycleLocalClient api = getTestClient();

                var variable = api.Variable(null, "some_key", true).Result;
            });
        }

        [TestMethod]
        public void User_NullUserId_ThrowsException()
        {
            Assert.ThrowsException<ArgumentException>(() => { _ = new DevCycleUser(); });
        }

        [TestMethod]
        public void User_InvalidUserIdLength_ThrowsException()
        {
            Assert.ThrowsException<ArgumentException>(() =>
            {
                _ = new DevCycleUser("Oy0mkUHONE6Qg36DhrOrwbvkCaxiMQPClHsELgFdfdlYCcE0AGyJqgl2tnV6Ago2"
                                     + "7uUXlXvChzLiLHPGRDavA9H82lM47B1pFOW51KQhT9kxLU1PgLfs2NOlekOWldtT9jh"
                                     + "JdgsDl0Cm49Vb7utlc4y0dyHYS1GKFuJwuipzVSrlYij39D8BWKLDbkqiJGc7qU2xCAeJv");
            });
        }

        [TestMethod]
        public void SetClientCustomDataTest()
        {
            using DevCycleLocalClient api = getTestClient();

            Dictionary<string, object> customData = new Dictionary<string, object>();
            customData.Add("strProp", "value");
            customData.Add("numProp", 100);
            customData.Add("boolProp", true);
            customData.Add("nullProp", null);

            api.SetClientCustomData(customData);
        }

        [TestMethod]
        public void OpenFeatureUserParsing()
        {
            EvaluationContext ctx = EvaluationContext.Builder()
                .Set("user_id", "test")
                .Set("customData",
                    new Structure(new Dictionary<string, Value> { { "customkey", new Value("customValue") } }))
                .Set("privateCustomData",
                    new Structure(new Dictionary<string, Value>
                        { { "privateCustomKey", new Value("privateCustomValue") } }))
                .Set("email", "email@email.com")
                .Set("name", "Name Name")
                .Set("language", "EN")
                .Set("country", "CA")
                .Set("appVersion", "0.0.1")
                .Set("appBuild", 1)
                .Set("nonSetValueBubbledCustomData", true)
                .Set("nonSetValueBubbledCustomData2", "true")
                .Set("nonSetValueBubbledCustomData3", 1)
                .Set("nonSetValueBubbledCustomData4", new Value((object)null))
                .Build();

            DevCycleUser user = DevCycleUser.FromEvaluationContext(ctx);

            Assert.AreEqual(user.UserId, ctx.GetValue("user_id").AsString);
            Assert.AreEqual(user.CustomData["customkey"], "customValue");
            Assert.AreEqual(user.PrivateCustomData["privateCustomKey"], "privateCustomValue");
            Assert.AreEqual(user.Email, "email@email.com");
            Assert.AreEqual(user.Name, "Name Name");
            Assert.AreEqual(user.Language, "EN");
            Assert.AreEqual(user.Country, "CA");
            Assert.AreEqual(user.AppVersion, "0.0.1");
            Assert.AreEqual(user.AppBuild, 1);
            Assert.AreEqual(user.CustomData["nonSetValueBubbledCustomData"], true);
            Assert.AreEqual(user.CustomData["nonSetValueBubbledCustomData2"], "true");
            Assert.AreEqual(user.CustomData["nonSetValueBubbledCustomData3"], 1d);
            Assert.AreEqual(user.CustomData["nonSetValueBubbledCustomData4"], null);


            ctx = EvaluationContext.Builder().Set("targetingKey", "test").Build();
            user = DevCycleUser.FromEvaluationContext(ctx);
            Assert.AreEqual(user.UserId, ctx.GetValue("targetingKey").AsString);
        }

        [TestMethod]
        public async Task TestOpenFeatureInitialization()
        {
            var dvcClient = getTestClient();
            await OpenFeature.Api.Instance.SetProviderAsync(dvcClient.GetOpenFeatureProvider());
            FeatureClient client = OpenFeature.Api.Instance.GetClient();

            var ctx = EvaluationContext.Builder().Set("user_id", "j_test").Build();
            var isEnabled = await client.GetBooleanValueAsync("test", false, ctx);
            Assert.IsTrue(isEnabled);
        }

        [TestMethod]
        public async Task TestOpenFeatureJSON()
        {
            using DevCycleLocalClient api = getTestClient();
            await OpenFeature.Api.Instance.SetProviderAsync(api.GetOpenFeatureProvider());
            FeatureClient client = OpenFeature.Api.Instance.GetClient();

            string key = "json";
            var ctx = EvaluationContext.Builder().Set("user_id", "j_test").Build();

            string json = "{\"key\": \"value\"}";

            var jsonDict = new Dictionary<string, Value>() { { "key", new Value("value") } };

            var defaultV = new Value(new Structure(jsonDict));
            var variable = await client.GetObjectDetailsAsync("json", defaultV, ctx);
            Assert.IsNotNull(variable);
            Assert.AreEqual(defaultV.IsStructure, variable.Value.IsStructure);
            Assert.AreEqual(defaultV.AsStructure.GetValue("key").AsString,
                variable.Value.AsStructure.GetValue("key").AsString);
            Assert.AreEqual( Reason.Default, variable.Reason);
        }

        [TestMethod]
        public void TestOpenFeatureSerialization()
        {
            var simpleDict = new Dictionary<string, Value>() { { "key", new Value("value") } };
            var listDict = new List<Value>()
            {
                new(new Structure(simpleDict)),
                new(new Structure(simpleDict)),
                new(new Structure(simpleDict))
            };
            var jsonDict = new Dictionary<string, Value>()
            {
                { "key", new Value("value") },
                { "key2", new Value(new Structure(simpleDict)) },
                { "listKey", new Value(listDict) },
                {"numberKey", new Value(1d)}
            };

            var defaultV = new Value(new Structure(jsonDict));
            var jsonString = JsonSerializer.Serialize(defaultV,
                new JsonSerializerOptions()
                    { WriteIndented = true, Converters = { new OpenFeatureValueJsonConverter() } });
            var deserialzed = JsonSerializer.Deserialize<Value>(jsonString, new JsonSerializerOptions()
                { WriteIndented = true, Converters = { new OpenFeatureValueJsonConverter() } });
            Console.WriteLine(jsonString);
            Assert.AreEqual(defaultV.IsStructure, deserialzed.IsStructure);
            Assert.AreEqual(defaultV.AsStructure.GetValue("key").AsString,
                deserialzed.AsStructure.GetValue("key").AsString);
            Assert.AreEqual(1, deserialzed.AsStructure.GetValue("numberKey").AsInteger);
        }

        [TestMethod]
        public async Task BeforeHookError_ThrowsException()
        {
            const string key = "test";
            using DevCycleLocalClient api = getTestClient();
            TestEvalHook hook = new TestEvalHook() { ThrowBefore = true };
            api.AddEvalHook(hook);
            
            var result = await api.VariableAsync(new DevCycleUser("test"), key, true); 
            
            Assert.AreEqual(1, hook.BeforeCallCount);
            Assert.AreEqual(0, hook.AfterCallCount);
            Assert.AreEqual(1, hook.ErrorCallCount);
            Assert.AreEqual(1, hook.FinallyCallCount);
            Assert.IsNotNull(result);
            Assert.AreEqual(key, result.Key);
            Assert.AreEqual(true, result.DefaultValue);
            Assert.AreEqual(TypeEnum.Boolean, result.Type);
            Assert.IsFalse(result.IsDefaulted);
        }

        [TestMethod]
        public async Task AfterHookError_ThrowsException()
        {
            const string key = "test";
            using DevCycleLocalClient api = getTestClient();
            TestEvalHook hook = new TestEvalHook() { ThrowAfter = true };
            api.AddEvalHook(hook);
            
            await Task.Delay(3000);
            var result = await api.VariableAsync(new DevCycleUser("test"), key, true); 

            Assert.AreEqual(1, hook.BeforeCallCount);
            Assert.AreEqual(1, hook.AfterCallCount);
            Assert.AreEqual(1, hook.ErrorCallCount);
            Assert.AreEqual(1, hook.FinallyCallCount);
            Assert.IsNotNull(result);
            Assert.AreEqual(key, result.Key);
            Assert.AreEqual(true, result.DefaultValue);
            Assert.AreEqual(TypeEnum.Boolean, result.Type);
            Assert.IsFalse(result.IsDefaulted);
        }

        [TestMethod]
        public async Task ErrorHookError_ThrowsException()
        {
            const string key = "test";
            using DevCycleLocalClient api = getTestClient();
            TestEvalHook hook = new TestEvalHook() { ThrowError = true, ThrowAfter = true };
            api.AddEvalHook(hook);
            
            await Task.Delay(3000);
            var result = await api.VariableAsync(new DevCycleUser("test"), key, true); 

            Assert.AreEqual(1, hook.BeforeCallCount);
            Assert.AreEqual(1, hook.AfterCallCount);
            Assert.AreEqual(1, hook.ErrorCallCount);
            Assert.AreEqual(1, hook.FinallyCallCount);
            Assert.IsNotNull(result);
            Assert.AreEqual(key, result.Key);
            Assert.AreEqual(true, result.DefaultValue);
            Assert.AreEqual(TypeEnum.Boolean, result.Type);
            Assert.IsFalse(result.IsDefaulted);
        }

        [TestMethod]
        public async Task FinallyHookError_ThrowsException()
        {
            const string key = "test";
            using DevCycleLocalClient api = getTestClient();
            TestEvalHook hook = new TestEvalHook() { ThrowFinally = true };
            api.AddEvalHook(hook);
            
            await Task.Delay(3000);
            var result = await api.VariableAsync(new DevCycleUser("test"), key, true); 

            Assert.AreEqual(1, hook.BeforeCallCount);
            Assert.AreEqual(1, hook.AfterCallCount);
            Assert.AreEqual(0, hook.ErrorCallCount);
            Assert.AreEqual(1, hook.FinallyCallCount);
            Assert.IsNotNull(result);
            Assert.AreEqual(key, result.Key);
            Assert.AreEqual(true, result.DefaultValue);
            Assert.AreEqual(TypeEnum.Boolean, result.Type);
            Assert.IsFalse(result.IsDefaulted);
        }

        [TestMethod]
        public async Task EvalHooks_NormalExecution()
        {
            const string key = "test";
            using DevCycleLocalClient api = getTestClient();
            TestEvalHook hook = new TestEvalHook();
            api.AddEvalHook(hook);
            
            await Task.Delay(3000);
            var result = await api.VariableAsync(new DevCycleUser("test"), key, true); 

            Assert.AreEqual(1, hook.BeforeCallCount);
            Assert.AreEqual(1, hook.AfterCallCount);
            Assert.AreEqual(0, hook.ErrorCallCount);
            Assert.AreEqual(1, hook.FinallyCallCount);
            Assert.IsNotNull(result);
            Assert.AreEqual(key, result.Key);
            Assert.AreEqual(true, result.DefaultValue);
            Assert.AreEqual(TypeEnum.Boolean, result.Type);
            Assert.IsFalse(result.IsDefaulted);
        }

        [TestMethod]
        public async Task ClearEvalHooks_RemovesAllHooks()
        {
            const string key = "test";
            using DevCycleLocalClient api = getTestClient();
            TestEvalHook hook1 = new TestEvalHook();
            TestEvalHook hook2 = new TestEvalHook();
            api.AddEvalHook(hook1);
            api.AddEvalHook(hook2);
            api.ClearEvalHooks();
            
            await Task.Delay(3000);
            var result = await api.VariableAsync(new DevCycleUser("test"), key, true); 

            Assert.AreEqual(0, hook1.BeforeCallCount);
            Assert.AreEqual(0, hook1.AfterCallCount);
            Assert.AreEqual(0, hook1.ErrorCallCount);
            Assert.AreEqual(0, hook1.FinallyCallCount);
            Assert.AreEqual(0, hook2.BeforeCallCount);
            Assert.AreEqual(0, hook2.AfterCallCount);
            Assert.AreEqual(0, hook2.ErrorCallCount);
            Assert.AreEqual(0, hook2.FinallyCallCount);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task EvalHooks_PassedInOptions()
        {
            const string key = "test";
            TestEvalHook hook = new TestEvalHook();
            var options = new DevCycleLocalOptions
            {
                EvalHooks = [hook]
            };
            using DevCycleLocalClient api = getTestClient(options);
            
            await Task.Delay(3000);
            var result = await api.VariableAsync(new DevCycleUser("test"), key, true);

            Assert.AreEqual(1, hook.BeforeCallCount);
            Assert.AreEqual(1, hook.AfterCallCount); 
            Assert.AreEqual(0, hook.ErrorCallCount);
            Assert.AreEqual(1, hook.FinallyCallCount);
            Assert.IsNotNull(result);
            Assert.AreEqual(key, result.Key);
            Assert.AreEqual(true, result.DefaultValue);
            Assert.AreEqual(TypeEnum.Boolean, result.Type);
            Assert.IsFalse(result.IsDefaulted);
        }

        [TestMethod]
        public async Task EvalHooks_MultipleHooksInOptions()
        {
            const string key = "test";
            TestEvalHook hook1 = new TestEvalHook();
            TestEvalHook hook2 = new TestEvalHook();
            var options = new DevCycleLocalOptions
            {
                EvalHooks = [hook1, hook2]
            };
            using DevCycleLocalClient api = getTestClient(options);
            
            await Task.Delay(3000);
            var result = await api.VariableAsync(new DevCycleUser("test"), key, true);

            Assert.AreEqual(1, hook1.BeforeCallCount);
            Assert.AreEqual(1, hook1.AfterCallCount);
            Assert.AreEqual(0, hook1.ErrorCallCount);
            Assert.AreEqual(1, hook1.FinallyCallCount);
            Assert.AreEqual(1, hook2.BeforeCallCount);
            Assert.AreEqual(1, hook2.AfterCallCount);
            Assert.AreEqual(0, hook2.ErrorCallCount);
            Assert.AreEqual(1, hook2.FinallyCallCount);
            Assert.IsNotNull(result);
        }
    }
}