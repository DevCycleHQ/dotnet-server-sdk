using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DevCycle.SDK.Server.Common.Model.Local
{
    [DataContract(Name = "requestEvent")]
    public class DVCRequestEvent
    {
        private const string VariableEvaluated = "variableEvaluated";
        private const string VariableDefaulted = "variableDefaulted";

        private readonly List<string> eventTypes = new List<string> {VariableEvaluated, VariableDefaulted};
        
        [DataMember(Name="type", EmitDefaultValue=false)]
        public string Type { get; }
        
        [DataMember(Name="target", EmitDefaultValue=false)]
        public string Target { get; }
        
        [DataMember(Name="customType", EmitDefaultValue=false)]
        public string CustomType { get; }
        
        [DataMember(Name="user_id", EmitDefaultValue=false)]
        public string UserId { get; }
        
        [DataMember(Name="date", EmitDefaultValue=false)]
        public long Date { get; }
        
        [DataMember(Name="clientDate", EmitDefaultValue=false)]
        public long? ClientDate { get; }
        
        [DataMember(Name="value", EmitDefaultValue=false)]
        public decimal? Value { get; set; }
        
        [DataMember(Name="featureVars", EmitDefaultValue=false)]
        public Dictionary<string, string> FeatureVars { get; }
        
        [DataMember(Name="metaData", EmitDefaultValue=false)]
        public Dictionary<string, object> MetaData { get; }
        
        public bool IsCustomEvent { get; }

        public DVCRequestEvent(Event @event, string userId, Dictionary<string, string> featureVars)
        {
            if (@event.Type == null)
            {
                throw new ArgumentException("Type cannot be null");
            }
            if (userId == null)
            {
                throw new ArgumentException("UserId cannot be null");
            }

            if (!eventTypes.Contains(@event.Type))
            {
                IsCustomEvent = true;
            }

            Type = IsCustomEvent ? "customEvent" : @event.Type;
            Target = @event.Target;
            CustomType = IsCustomEvent ? @event.Type : null;
            UserId = userId;
            Date = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            ClientDate = @event.Date ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Value = @event.Value;
            FeatureVars = featureVars;
            MetaData = @event.MetaData;
        }
    }
}