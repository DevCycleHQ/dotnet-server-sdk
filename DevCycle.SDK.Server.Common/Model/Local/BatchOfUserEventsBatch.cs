using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DevCycle.SDK.Server.Common.Model.Local
{
    [DataContract]
    public class BatchOfUserEventsBatch
    {
        public BatchOfUserEventsBatch(List<UserEventsBatchRecord> userEventsBatchRecords)
        {
            UserEventsBatchRecords = userEventsBatchRecords;
        }
        
        [DataMember(Name="batch", EmitDefaultValue=false)]
        public List<UserEventsBatchRecord> UserEventsBatchRecords { get; private set; }
    }
}