using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace DevCycle.SDK.Server.Common.Model.Local
{
    public class OptInColorSettings
    {
        [DataMember(Name = "primary", EmitDefaultValue = false)]
        [JsonProperty("primary")]
        public bool Primary { get; set; }

        [DataMember(Name = "secondary", EmitDefaultValue = false)]
        [JsonProperty("secondary")]
        public bool Secondary { get; set; }
    }
}