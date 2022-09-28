

namespace DevCycle.SDK.Server.Local.MSTests
{
    public class Fixtures
    {
        public static string FeatureId = "6216422850294da359385e8b";
        public static string VariationOnId = "6216422850294da359385e8f";
        public static string VariationOffId = "6216422850294da359385e90";
        public static string VariableId = "6216422850294da359385e8d";
        public static string VariableKey = "test";
        public static string Config()
        {
            string configString = "{\"project\":{\"settings\":{\"edgeDB\":{\"enabled\":false}},\"_id\":\"6216420c2ea68943c8833c09\",\"key\":\"default\",\"a0_organization\":\"org_NszUFyWBFy7cr95J\"},\"environment\":{\"_id\":\"6216420c2ea68943c8833c0b\",\"key\":\"development\"},\"features\":[{\"_id\":\"" + FeatureId + "\",\"key\":\"" + VariableKey + "\",\"type\":\"release\",\"variations\":[{\"variables\":[{\"_var\":\"" + VariableId + "\",\"value\":true}],\"name\":\"Variation On\",\"key\":\"variation-on\",\"_id\":\"" + VariationOnId + "\"},{\"variables\":[{\"_var\":\"" + VariableId + "\",\"value\":false}],\"name\":\"Variation Off\",\"key\":\"variation-off\",\"_id\":\"" + VariationOffId + "\"}],\"configuration\":{\"_id\":\"621642332ea68943c8833c4a\",\"targets\":[{\"distribution\":[{\"percentage\":0.5,\"_variation\":\"" + VariationOnId + "\"},{\"percentage\":0.5,\"_variation\":\"" + VariationOffId + "\"}],\"_audience\":{\"_id\":\"621642332ea68943c8833c4b\",\"filters\":{\"operator\":\"and\",\"filters\":[{\"values\":[],\"type\":\"all\",\"filters\":[]}]}},\"_id\":\"621642332ea68943c8833c4d\"}],\"forcedUsers\":{}}}],\"variables\":[{\"_id\":\"" + VariableId + "\",\"key\":\"" + VariableKey + "\",\"type\":\"Boolean\"}],\"featureVariationMap\":{\"" + FeatureId + "\":\"" + VariationOnId + "\"},\"variableVariationMap\":{\"" + VariableKey + "\":\"" + VariationOnId + "\"},\"variableHashes\":{\"" + VariableKey + "\":2447239932}}";
            return configString;
        }
    }
}