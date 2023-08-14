using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace DevCycle.SDK.Server.Common.Model.Local
{
    [DataContract]
    public class UserEventsBatchRecord
    {
        [DataMember(Name="user", EmitDefaultValue=false)]
        [JsonProperty("user")]
        public DevCyclePopulatedUser User { get; set;  }
        
        [DataMember(Name="events", EmitDefaultValue=false)]
        [JsonProperty("events")]
        public List<DevCycleRequestEvent> Events { get; private set; }
    }
}