using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace DevCycle.SDK.Server.Common.Model.Local
{
    public class FlushPayload
    {
        [DataMember(Name = "payloadId", EmitDefaultValue = false)]
        [JsonProperty("payloadId")]
        public string PayloadID { get; set; }
        
        [DataMember(Name = "status", EmitDefaultValue = false)]
        [JsonProperty("status")]
        public string Status { get; set; }

        [DataMember(Name = "eventCount", EmitDefaultValue = false)]
        [JsonProperty("eventCount")]
        public int EventCount { get; set; }

        [DataMember(Name = "records", EmitDefaultValue = false)]
        [JsonProperty("records")]
        public List<UserEventsBatchRecord> Records { get; set; }
    }
}