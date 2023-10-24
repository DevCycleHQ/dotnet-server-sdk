using System.Threading.Tasks;
using DevCycle.SDK.Server.Common.Model;
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

        public override async Task<ResolutionDetails<bool>> ResolveBooleanValue(string flagKey, bool defaultValue,
            EvaluationContext context = null)
        {
            // CONCEPT?
            DevCycleUser user = DevCycleUser.FromEvaluationContext(context);
            var variable= await Client.Variable(user, flagKey, defaultValue);
            return variable.ToOpenFeature();
        }

        public override Task<ResolutionDetails<string>> ResolveStringValue(string flagKey, string defaultValue,
            EvaluationContext context = null)
        {
            throw new System.NotImplementedException();
        }

        public override Task<ResolutionDetails<int>> ResolveIntegerValue(string flagKey, int defaultValue,
            EvaluationContext context = null)
        {
            throw new System.NotImplementedException();
        }

        public override Task<ResolutionDetails<double>> ResolveDoubleValue(string flagKey, double defaultValue,
            EvaluationContext context = null)
        {
            throw new System.NotImplementedException();
        }

        public override Task<ResolutionDetails<Value>> ResolveStructureValue(string flagKey, Value defaultValue,
            EvaluationContext context = null)
        {
            throw new System.NotImplementedException();
        }
    }
}