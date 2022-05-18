using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DevCycle.SDK.Server.Common.Model.Local
{
    [DataContract]
    public class UserEventsBatchRecord
    {
        [DataMember(Name="user", EmitDefaultValue=false)]
        public DVCPopulatedUser User { get; set; }
        
        [DataMember(Name="events", EmitDefaultValue=false)]
        public List<DVCRequestEvent> Events { get; private set; }

        public UserEventsBatchRecord(DVCPopulatedUser user)
        {
            User = user;
            Events = new List<DVCRequestEvent>();
        }
    }
}