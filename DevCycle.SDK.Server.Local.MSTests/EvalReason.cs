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
using Newtonsoft.Json;
using RichardSzalay.MockHttp;

namespace DevCycle.SDK.Server.Local.MSTests;

[TestClass]
public class EvalReasonTests
{
    private DevCycleLocalClient getTestClient(DevCycleLocalOptions options = null, string config = null,
        bool skipInitialize = false)
    {
        config ??= new string(Fixtures.Config());

        var mockHttp = new MockHttpMessageHandler();

        mockHttp.When("https://config-cdn*")
            .Respond(skipInitialize ? HttpStatusCode.BadRequest : HttpStatusCode.OK, "application/json",
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

    // ===== Tests for Default Reasons - Variable Method =====

    [TestMethod]
    public async Task Variable_MissingConfig_ReturnsDefaultWithMissingConfigReason()
    {
        using DevCycleLocalClient api = getTestClient(skipInitialize: true);
        var user = new DevCycleUser("test_user");
        const string key = "test_variable";
        const bool defaultValue = false;

        var result = await api.Variable(user, key, defaultValue);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsDefaulted);
        Assert.AreEqual(EvalReasons.DEFAULT, result.Eval.Reason);
        Assert.AreEqual(DefaultReasonDetails.MissingConfig, result.Eval.Details);
    }

    [TestMethod]
    public async Task Variable_UserNotTargeted_ReturnsDefaultWithUserNotTargetedReason()
    {
        using DevCycleLocalClient api = getTestClient();
        var user = new DevCycleUser("test_user");
        const string key = "non_existent_variable";
        const string defaultValue = "default_string";

        var result = await api.Variable(user, key, defaultValue);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsDefaulted);
        Assert.AreEqual(EvalReasons.DEFAULT, result.Eval.Reason);
        Assert.AreEqual(DefaultReasonDetails.UserNotTargeted, result.Eval.Details);
    }

    // ===== Tests for Default Reasons - VariableAsync Method =====

    [TestMethod]
    public async Task VariableAsync_MissingConfig_ReturnsDefaultWithMissingConfigReason()
    {
        using DevCycleLocalClient api = getTestClient(skipInitialize: true);
        var user = new DevCycleUser("test_user");
        const string key = "test_variable";
        const int defaultValue = 42;

        var result = await api.VariableAsync(user, key, defaultValue);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsDefaulted);
        Assert.AreEqual(EvalReasons.DEFAULT, result.Eval.Reason);
        Assert.AreEqual(DefaultReasonDetails.MissingConfig, result.Eval.Details);
    }

    [TestMethod]
    public async Task VariableAsync_UserNotTargeted_ReturnsDefaultWithUserNotTargetedReason()
    {
        using DevCycleLocalClient api = getTestClient();
        var user = new DevCycleUser("test_user");
        const string key = "non_existent_variable";
        const double defaultValue = 3.14;

        var result = await api.VariableAsync(user, key, defaultValue);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsDefaulted);
        Assert.AreEqual(EvalReasons.DEFAULT, result.Eval.Reason);
        Assert.AreEqual(DefaultReasonDetails.UserNotTargeted, result.Eval.Details);
    }

    // ===== Start test section - validate mismatch types return null from wasm, so we cannot provide accurate eval reason =====
    [TestMethod]
    public async Task Variable_Bool_MismatchType_ReturnsDefault()
    {
        using DevCycleLocalClient api = getTestClient();
        var user = new DevCycleUser("test_user");
        const string key = "test";
        const bool validDefaultValue = false;
        const string invalidDefaultValue = "default";

        var validResult = await api.Variable(user, key, validDefaultValue);
        Assert.IsFalse(validResult.IsDefaulted);

        var invalidResult = await api.Variable(user, key, invalidDefaultValue);
        Assert.IsTrue(invalidResult.IsDefaulted);
        Assert.AreEqual(EvalReasons.DEFAULT, invalidResult.Eval.Reason);
        Assert.AreEqual(DefaultReasonDetails.UserNotTargeted, invalidResult.Eval.Details);
    }

    [TestMethod]
    public async Task Variable_JSON_MismatchType_ReturnsDefault()
    {
        using DevCycleLocalClient api = getTestClient(config: Fixtures.ConfigWithJSONValues());
        var user = new DevCycleUser("test_user");
        const string key = "test";
        var validDefaultValue = Newtonsoft.Json.Linq.JObject.Parse("{\"key\": \"default\"}");
        const int invalidDefaultValue = 0;

        var validResult = await api.Variable(user, key, validDefaultValue);
        Assert.IsFalse(validResult.IsDefaulted);

        var invalidResult = await api.Variable(user, key, invalidDefaultValue);
        Assert.IsTrue(invalidResult.IsDefaulted);
        Assert.AreEqual(EvalReasons.DEFAULT, invalidResult.Eval.Reason);
        Assert.AreEqual(DefaultReasonDetails.UserNotTargeted, invalidResult.Eval.Details);
    }
    // ==== End of test section ====

    [TestMethod]
    public void EvalReasonConstructor_HandlesInvalidReasonEnum()
    {
        var evalObj = new EvalReason("invalidReason", "invalidDetails", "someTargetId");
        Assert.IsNotNull(evalObj);
        Assert.AreEqual(EvalReasons.UNKNOWN, evalObj.Reason);
    }

    [TestMethod]
    public void JSONSerializes_WhenValid()
    {
        const string jsonString = @"{""reason"": ""TARGETING_MATCH"", ""details"": ""Missing Config"", ""target_id"": ""test-target""}";
        var evalObj = JsonConvert.DeserializeObject<EvalReason>(jsonString);
        Assert.IsNotNull(evalObj);
        Assert.AreEqual(EvalReasons.TARGETING_MATCH, evalObj.Reason);
        Assert.AreEqual("Missing Config", evalObj.Details);
        Assert.AreEqual("test-target", evalObj.TargetId);
    }

    [TestMethod]
    public void JSONSerializes_WhenInValid()
    {
        const string jsonString = @"{""reason"": ""REASON_NOT_IN_ENUM"", ""UnexpectedProp"": 1}";
        var evalObj = JsonConvert.DeserializeObject<EvalReason>(jsonString);
        Assert.IsNotNull(evalObj);
        Assert.AreEqual(EvalReasons.UNKNOWN, evalObj.Reason);
    }
}
