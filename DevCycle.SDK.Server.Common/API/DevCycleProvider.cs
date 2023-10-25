using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Common.Model;
using Newtonsoft.Json.Linq;
using OpenFeature;
using OpenFeature.Model;

namespace DevCycle.SDK.Server.Common.API
{
    public class DevCycleProvider : FeatureProvider
    {
        private DevCycleBaseClient Client { get; }

        public DevCycleProvider(DevCycleBaseClient client)
        {
            Client = client;
        }

        public override Metadata GetMetadata()
        {
            return new Metadata(Client.SdkPlatform);
        }

        private async Task<ResolutionDetails<T>> EvaluateDevCycle<T>(string flagKey, T defaultValue,
            EvaluationContext context = null)
        {
            var user = DevCycleUser.FromEvaluationContext(context);
            var variable = await Client.Variable(user, flagKey, defaultValue);
            return variable.GetResolutionDetails();
        }

        public override async Task<ResolutionDetails<bool>> ResolveBooleanValue(string flagKey, bool defaultValue,
            EvaluationContext context = null)
        {
            return await EvaluateDevCycle(flagKey, defaultValue, context);
        }

        public override async Task<ResolutionDetails<string>> ResolveStringValue(string flagKey, string defaultValue,
            EvaluationContext context = null)
        {
            return await EvaluateDevCycle(flagKey, defaultValue, context);
        }

        public override async Task<ResolutionDetails<int>> ResolveIntegerValue(string flagKey, int defaultValue,
            EvaluationContext context = null)
        {
            return await EvaluateDevCycle(flagKey, defaultValue, context);
        }

        public override async Task<ResolutionDetails<double>> ResolveDoubleValue(string flagKey, double defaultValue,
            EvaluationContext context = null)
        {
            return await EvaluateDevCycle(flagKey, defaultValue, context);
        }

        public override async Task<ResolutionDetails<Value>> ResolveStructureValue(string flagKey, Value defaultValue,
            EvaluationContext context = null)
        {
            if (!defaultValue.IsStructure)
                throw new System.Exception("Cannot call ResolveStructureValue with non-structure Value's");
            var jsonString = JsonSerializer.Serialize(defaultValue,
                new JsonSerializerOptions() { Converters = { new OpenFeatureValueJsonConverter() } });

            var newtonsoftJObj = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonString);
            var user = DevCycleUser.FromEvaluationContext(context);
            var variable = await Client.Variable(user, flagKey, (JObject)newtonsoftJObj);
            // TODO: This parsing needs to return a ResolutionDetails<Value> - but currently needs conversion from
            return null;variable.GetResolutionDetails();
        }
    }
}