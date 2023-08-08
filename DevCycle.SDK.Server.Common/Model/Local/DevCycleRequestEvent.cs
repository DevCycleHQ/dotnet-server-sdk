using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace DevCycle.SDK.Server.Common.Model.Local
{
    [DataContract(Name = "requestEvent")]
    public class DevCycleRequestEvent
    {
        private readonly List<string> eventTypes = new List<string>
        {
            EventTypes.aggVariableEvaluated,
            EventTypes.variableEvaluated,
            EventTypes.aggVariableDefaulted,
            EventTypes.variableDefaulted
        };
        
        [DataMember(Name="type", EmitDefaultValue=false)]
        [JsonProperty("type")]
        public string Type { get; set; }
        
        [DataMember(Name="target", EmitDefaultValue=false)]
        [JsonProperty("target")]
        public string Target { get; set; }
        
        [DataMember(Name="customType", EmitDefaultValue=false)]
        [JsonProperty("customType")]
        public string CustomType { get; set; }
        
        [DataMember(Name="user_id", EmitDefaultValue=false)]
        [JsonProperty("user_id")]
        public string UserId { get; set; }
        
        [DataMember(Name="clientDate", EmitDefaultValue=false)]
        [JsonProperty("clientDate")]
        public DateTime ClientDate { get; set; }
        
        [DataMember(Name="value", EmitDefaultValue=false)]
        [JsonProperty("value")]
        public double Value { get; set; }
        
        [DataMember(Name="featureVars", EmitDefaultValue=false)]
        [JsonProperty("featureVars")]
        public Dictionary<string, string> FeatureVars { get; set; }
        
        [DataMember(Name="metaData", EmitDefaultValue=false)]
        [JsonProperty("metaData")]
        public Dictionary<string, object> MetaData { get; set; }

        public DevCycleRequestEvent()
        {
            
        }

        public DevCycleRequestEvent(DevCycleEvent @event, string userId, Dictionary<string, string> featureVars)
        {
            if (@event.Type == null)
            {
                throw new ArgumentException("Type cannot be null");
            }
            if (userId == null)
            {
                throw new ArgumentException("UserId cannot be null");
            }

            Type = !eventTypes.Contains(Type) ? "customEvent" : @event.Type;
            Target = @event.Target;
            UserId = userId;
            ClientDate = DateTimeOffset.UtcNow.DateTime;
            Value = @event.Value;
            FeatureVars = featureVars;
            MetaData = @event.MetaData;
        }
    }
}