using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace DevCycle.SDK.Server.Common.Model.Local
{
    public class FeatureVariation
    {
        [DataMember(Name = "_feature", EmitDefaultValue = false)]
        [JsonProperty("_feature")]
        public string Feature { get; set; }

        [DataMember(Name = "_variation", EmitDefaultValue = false)]
        [JsonProperty("_variation")]
        public string Variation { get; set; }
    }
}