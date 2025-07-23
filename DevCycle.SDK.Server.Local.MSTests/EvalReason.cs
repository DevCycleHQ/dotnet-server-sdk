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
using RichardSzalay.MockHttp;

namespace DevCycle.SDK.Server.Local.MSTests;

[TestClass]
public class EvalReason
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
        Assert.AreEqual(defaultValue, result.Value);
        Assert.AreEqual(key, result.Key);
        Assert.AreEqual(EvalReasons.Default, result.Eval.Reason);
        Assert.AreEqual(DefaultReasonDetails.MissingConfig, result.Eval.Details);
        Assert.AreEqual(TypeEnum.Boolean, result.Type);
    }

    [TestMethod]
    public async Task Variable_UserNotTargeted_ReturnsDefaultWithUserNotTargetedReason()
    {
        using DevCycleLocalClient api = getTestClient();
        var user = new DevCycleUser("test_user");
        const string key = "non_existent_variable";
        const string defaultValue = "default_string";
        await Task.Delay(3000);

        var result = await api.Variable(user, key, defaultValue);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsDefaulted);
        Assert.AreEqual(defaultValue, result.Value);
        Assert.AreEqual(key, result.Key);
        Assert.AreEqual(EvalReasons.Default, result.Eval.Reason);
        Assert.AreEqual(DefaultReasonDetails.UserNotTargeted, result.Eval.Details);
        Assert.AreEqual(TypeEnum.String, result.Type);
    }

    [TestMethod]
    public async Task Variable_TypeMismatch_ReturnsDefaultWithTypeMismatchReason()
    {
        using DevCycleLocalClient api = getTestClient();
        var user = new DevCycleUser("j_test");
        const string key = "test"; // This is configured as Boolean in fixtures
        // const bool defaultValue = true; // Requesting as string instead of boolean
        const string defaultValue = "test"; // Requesting as string instead of boolean
        await Task.Delay(3000);

        var result = await api.Variable(user, key, defaultValue);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsDefaulted);
        Assert.AreEqual(defaultValue, result.Value);
        Assert.AreEqual(key, result.Key);
        Assert.AreEqual(EvalReasons.Default, result.Eval.Reason);
        Assert.AreEqual(DefaultReasonDetails.TypeMismatch, result.Eval.Details);
        Assert.AreEqual(TypeEnum.String, result.Type);
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
        Assert.AreEqual(defaultValue, result.Value);
        Assert.AreEqual(key, result.Key);
        Assert.AreEqual(EvalReasons.Default, result.Eval.Reason);
        Assert.AreEqual(DefaultReasonDetails.MissingConfig, result.Eval.Details);
        Assert.AreEqual(TypeEnum.Number, result.Type);
    }

    [TestMethod]
    public async Task VariableAsync_UserNotTargeted_ReturnsDefaultWithUserNotTargetedReason()
    {
        using DevCycleLocalClient api = getTestClient();
        var user = new DevCycleUser("test_user");
        const string key = "non_existent_variable";
        const double defaultValue = 3.14;
        await Task.Delay(3000);

        var result = await api.VariableAsync(user, key, defaultValue);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsDefaulted);
        Assert.AreEqual(defaultValue, result.Value);
        Assert.AreEqual(key, result.Key);
        Assert.AreEqual(EvalReasons.Default, result.Eval.Reason);
        Assert.AreEqual(DefaultReasonDetails.UserNotTargeted, result.Eval.Details);
        Assert.AreEqual(TypeEnum.Number, result.Type);
    }

    [TestMethod]
    public async Task VariableAsync_TypeMismatch_ReturnsDefaultWithTypeMismatchReason()
    {
        using DevCycleLocalClient api = getTestClient();
        var user = new DevCycleUser("j_test");
        const string key = "test"; // This is configured as Boolean in fixtures
        const string defaultValue = "test"; // Requesting as number instead of boolean
        await Task.Delay(3000);

        var result = await api.VariableAsync(user, key, defaultValue);

        Assert.IsNotNull(result);
        //Assert.IsTrue(result.IsDefaulted);
        // Assert.AreEqual(defaultValue, result.Value);
        Assert.AreEqual(key, result.Key);
        Assert.AreEqual(EvalReasons.Default, result.Eval.Reason);
        Assert.AreEqual(DefaultReasonDetails.TypeMismatch, result.Eval.Details);
        Assert.AreEqual(TypeEnum.Number, result.Type);
    }
}