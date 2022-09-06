using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace DevCycle.SDK.Server.Common.Model.Local
{
    public class ProjectSettings
    {
        [DataMember(Name = "edgeDB", EmitDefaultValue = false)]
        [JsonProperty("edgeDB")]
        public EdgeDBSettings EdgeDB { get; set; }

        [DataMember(Name = "sdkSettings", EmitDefaultValue = false, IsRequired = false)]
        [JsonProperty("sdkSettings")]
        public SdkSettings SdkSettings { get; set; }

        [DataMember(Name = "optIn", EmitDefaultValue = false, IsRequired = false)]
        [JsonProperty("optIn")]
        public OptInSettings OptIn { get; set; }
    }
}