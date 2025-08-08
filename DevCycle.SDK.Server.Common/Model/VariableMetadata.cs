using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace DevCycle.SDK.Server.Common.Model
{
    [DataContract]
    public class VariableMetadata
    {
        [DataMember(Name = "FeatureId", EmitDefaultValue = false)]
        [JsonProperty("featureId")]
        public string FeatureId { get; set; }
    }

}