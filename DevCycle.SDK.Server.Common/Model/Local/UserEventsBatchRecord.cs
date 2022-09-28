using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace DevCycle.SDK.Server.Common.Model.Local
{
    [DataContract]
    public class UserEventsBatchRecord
    {
        [DataMember(Name="user", EmitDefaultValue=false, IsRequired = false)]
        [JsonProperty("user")]

        public DVCPopulatedUser User { get; }
        
        [DataMember(Name="events", EmitDefaultValue=false)]
        [JsonProperty("events")]

        public List<DVCRequestEvent> Events { get; private set; }

        public UserEventsBatchRecord(DVCPopulatedUser user)
        {
            User = user;
            Events = new List<DVCRequestEvent>();
        }
    }
}