using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace DevCycle.SDK.Server.Common.Model.Local
{
    public class SdkSettings
    {
        [DataMember(Name = "eventQueueLimit", EmitDefaultValue = false)]
        [JsonProperty("eventQueueLimit")]
        public int EventQueueLimit { get; set; }
    }
}