using System.Threading.Tasks;
using DevCycle.SDK.Server.Local.Api;
using DevCycle.SDK.Server.Common.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace DevCycle.SDK.Server.Local.MSTests;

[TestClass]
public class EvalReasonTests
{
    // ===== Tests for Default Reasons - Variable Method =====
    [TestMethod]
    public async Task Variable_MissingConfig_ReturnsDefaultWithMissingConfigReason()
    {
        using DevCycleLocalClient api = DevCycleTestClient.getTestClient(skipInitialize: true);
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
        using DevCycleLocalClient api = DevCycleTestClient.getTestClient();
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
        using DevCycleLocalClient api = DevCycleTestClient.getTestClient(skipInitialize: true);
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
        using DevCycleLocalClient api = DevCycleTestClient.getTestClient();
        var user = new DevCycleUser("test_user");
        const string key = "non_existent_variable";
        const double defaultValue = 3.14;

        var result = await api.VariableAsync(user, key, defaultValue);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsDefaulted);
        Assert.AreEqual(EvalReasons.DEFAULT, result.Eval.Reason);
        Assert.AreEqual(DefaultReasonDetails.UserNotTargeted, result.Eval.Details);
    }

    // ===== Tests for Default Reasons - VariableAsync Method =====
    [TestMethod]
    public async Task OF_Variable_MissingConfig_ReturnsDefaultWithMissingConfigReason()
    {
        using DevCycleBaseClient devCycleClient = getTestClient(skipInitialize: true);
        await OpenFeature.Api.Instance.SetProviderAsync(devCycleClient.GetOpenFeatureProvider());
        FeatureClient oFeatureClient = OpenFeature.Api.Instance.GetClient();
        var ctx = EvaluationContext.Builder().Set("user_id", "test_user").Build();
        const string key = "test";
        const bool defaultValue = false;

        var result = await oFeatureClient.GetBooleanDetailsAsync(key, defaultValue, ctx);

        Assert.IsNotNull(result);
        Assert.AreEqual(defaultValue, result.Value);
        Assert.AreEqual(EvalReasons.DEFAULT, result.Reason);
        Assert.AreEqual(DefaultReasonDetails.MissingConfig, result.FlagMetadata?.GetString("evalReasonDetails"));
    }

    [TestMethod]
    public async Task OF_Variable_MissingConfig_ReturnsDefaultWithUserNotTargetedReason()
    {
        using DevCycleBaseClient devCycleClient = getTestClient();
        await OpenFeature.Api.Instance.SetProviderAsync(devCycleClient.GetOpenFeatureProvider());
        FeatureClient oFeatureClient = OpenFeature.Api.Instance.GetClient();
        var ctx = EvaluationContext.Builder().Set("user_id", "test_user").Build();
        const string key = "non_existent_variable";
        const bool defaultValue = false;

        var result = await oFeatureClient.GetBooleanDetailsAsync(key, defaultValue, ctx);

        Assert.IsNotNull(result);
        Assert.AreEqual(defaultValue, result.Value);
        Assert.AreEqual(EvalReasons.DEFAULT, result.Reason);
        Assert.AreEqual(DefaultReasonDetails.UserNotTargeted, result.FlagMetadata?.GetString("evalReasonDetails"));
    }

    // ===== Start test section - validate mismatch types return null from wasm, so we cannot provide accurate eval reason =====
    [TestMethod]
    public async Task Variable_Bool_MismatchType_ReturnsDefault()
    {
        using DevCycleLocalClient api = DevCycleTestClient.getTestClient();
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
        using DevCycleLocalClient api = DevCycleTestClient.getTestClient(config: Fixtures.ConfigWithJSONValues());
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
    public void JSONSerializes_WhenValid()
    {
        const string jsonString = @"{""reason"": ""TARGETING_MATCH"", ""details"": ""Missing Config"", ""target_id"": ""test-target""}";
        var evalObj = JsonConvert.DeserializeObject<EvalReason>(jsonString);
        Assert.IsNotNull(evalObj);
        Assert.AreEqual("TARGETING_MATCH", evalObj.Reason);
        Assert.AreEqual("Missing Config", evalObj.Details);
        Assert.AreEqual("test-target", evalObj.TargetId);
    }

    [TestMethod]
    public void JSONSerializes_WhenInValid()
    {
        const string jsonString = @"{""reason"": ""SOME_REASON"", ""UnexpectedProp"": 1}";
        var evalObj = JsonConvert.DeserializeObject<EvalReason>(jsonString);
        Assert.IsNotNull(evalObj);
        Assert.AreEqual("SOME_REASON", evalObj.Reason);
    }
}
