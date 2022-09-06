using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace DevCycle.SDK.Server.Common.Model.Local
{
    public class EdgeDBSettings
    {
        [DataMember(Name = "enabled", EmitDefaultValue = false)]
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }
    }
}