using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Common.Model;
using Newtonsoft.Json.Linq;
using OpenFeature;
using OpenFeature.Constant;
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

        public override async Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(string flagKey, bool defaultValue, EvaluationContext context = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return await EvaluateDevCycle(flagKey, defaultValue, context);
        }

        public override async Task<ResolutionDetails<string>> ResolveStringValueAsync(string flagKey, string defaultValue, EvaluationContext context = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return await EvaluateDevCycle(flagKey, defaultValue, context);
        }

        public override async Task<ResolutionDetails<int>> ResolveIntegerValueAsync(string flagKey, int defaultValue, EvaluationContext context = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return await EvaluateDevCycle(flagKey, defaultValue, context);
        }

        public override async Task<ResolutionDetails<double>> ResolveDoubleValueAsync(string flagKey, double defaultValue, EvaluationContext context = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return await EvaluateDevCycle(flagKey, defaultValue, context);
        }

        public override async Task<ResolutionDetails<Value>> ResolveStructureValueAsync(string flagKey, Value defaultValue, EvaluationContext context = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            if (!defaultValue.IsStructure)
                throw new System.Exception("Cannot call ResolveStructureValue with non-structure Value's");
            var jsonString = JsonSerializer.Serialize(defaultValue,
                new JsonSerializerOptions() { Converters = { new OpenFeatureValueJsonConverter() } });

            var newtonsoftJObj = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonString);
            var user = DevCycleUser.FromEvaluationContext(context);
            var variable = await Client.Variable(user, flagKey, (JObject)newtonsoftJObj);
            var jsonValue = Newtonsoft.Json.JsonConvert.SerializeObject(variable.Value);
            var openFeatureValue = JsonSerializer.Deserialize<Value>(jsonValue,
                new JsonSerializerOptions() { Converters = { new OpenFeatureValueJsonConverter() } });

            var details = variable.GetResolutionDetails()
            return new ResolutionDetails<Value>(flagKey, openFeatureValue, ErrorType.None, details.Reason, flagMetadata: details.FlagMetadata);
        }

        public override Task ShutdownAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            Client.Dispose();
            return Task.CompletedTask;
        }

        public override async void Track(string trackingEventName, EvaluationContext evaluationContext = null,
            TrackingEventDetails trackingEventDetails = null)
        {
            var e = DevCycleEvent.FromTrackingEventDetails(trackingEventDetails);
            e.Type = trackingEventName;
            await Client.Track(DevCycleUser.FromEvaluationContext(evaluationContext), e);
        }

        private async Task<ResolutionDetails<T>> EvaluateDevCycle<T>(string flagKey, T defaultValue,
            EvaluationContext context = null)
        {
            var user = DevCycleUser.FromEvaluationContext(context);
            var variable = await Client.Variable(user, flagKey, defaultValue);

            return variable.GetResolutionDetails();
        }

    }
}
