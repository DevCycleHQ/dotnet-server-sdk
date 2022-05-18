using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;

namespace DevCycle.SDK.Server.Common.Model
{
    [DataContract]
    public class Event : IEquatable<Event>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Event" /> class.
        /// </summary>
        /// <param name="type">Custom event type (required).</param>
        /// <param name="target">Custom event target / subject of event. Contextual to event type.</param>
        /// <param name="date">Unix epoch time the event occurred according to client.</param>
        /// <param name="value">Value for numerical events. Contextual to event type.</param>
        /// <param name="metaData">Extra JSON metadata for event. Contextual to event type.</param>
        public Event(string type = default, string target = default, long? date = default, long? value = default,
            Dictionary<string, object> metaData = default)
        {
            // to ensure "type" is required (not null)
            if (type == null)
            {
                throw new InvalidDataException("type is a required property for ModelEvent and cannot be null");
            }

            Type = type;

            Target = target;
            Date = date;
            Value = value;
            MetaData = metaData;
        }

        /// <summary>
        /// Custom event type
        /// </summary>
        /// <value>Custom event type</value>
        [DataMember(Name = "type", EmitDefaultValue = false)]
        public string Type { get; set; }

        /// <summary>
        /// Custom event target / subject of event. Contextual to event type
        /// </summary>
        /// <value>Custom event target / subject of event. Contextual to event type</value>
        [DataMember(Name = "target", EmitDefaultValue = false)]
        public string Target { get; set; }

        /// <summary>
        /// Unix epoch time the event occurred according to client
        /// </summary>
        /// <value>Unix epoch time the event occurred according to client</value>
        [DataMember(Name = "date", EmitDefaultValue = false)]
        public long? Date { get; set; }

        /// <summary>
        /// Value for numerical events. Contextual to event type
        /// </summary>
        /// <value>Value for numerical events. Contextual to event type</value>
        [DataMember(Name = "value", EmitDefaultValue = false)]
        public decimal? Value { get; set; }

        /// <summary>
        /// Extra JSON metadata for event. Contextual to event type
        /// </summary>
        /// <value>Extra JSON metadata for event. Contextual to event type</value>
        [DataMember(Name = "metaData", EmitDefaultValue = false)]
        public Dictionary<string, object> MetaData { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class Event {\n");
            sb.Append("  Type: ").Append(Type).Append("\n");
            sb.Append("  Target: ").Append(Target).Append("\n");
            sb.Append("  Date: ").Append(Date).Append("\n");
            sb.Append("  Value: ").Append(Value).Append("\n");
            sb.Append("  MetaData: ").Append(MetaData).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public virtual string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// Returns true if objects are equal
        /// </summary>
        /// <param name="input">Object to be compared</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object input)
        {
            return Equals(input as Event);
        }

        /// <summary>
        /// Returns true if ModelEvent instances are equal
        /// </summary>
        /// <param name="input">Instance of ModelEvent to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(Event input)
        {
            if (input == null)
                return false;

            return
                (
                    Type == input.Type ||
                    (Type != null &&
                     Type.Equals(input.Type))
                ) &&
                (
                    Target == input.Target ||
                    (Target != null &&
                     Target.Equals(input.Target))
                ) &&
                (
                    Date == input.Date ||
                    (Date != null &&
                     Date.Equals(input.Date))
                ) &&
                (
                    Value == input.Value ||
                    (Value != null &&
                     Value.Equals(input.Value))
                ) &&
                (
                    MetaData == input.MetaData ||
                    (MetaData != null &&
                     MetaData.Equals(input.MetaData))
                );
        }

        /// <summary>
        /// Gets the hash code
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hashCode = 41;
                if (Type != null)
                    hashCode = hashCode * 59 + Type.GetHashCode();
                if (Target != null)
                    hashCode = hashCode * 59 + Target.GetHashCode();
                if (Date != null)
                    hashCode = hashCode * 59 + Date.GetHashCode();
                if (Value != null)
                    hashCode = hashCode * 59 + Value.GetHashCode();
                if (MetaData != null)
                    hashCode = hashCode * 59 + MetaData.GetHashCode();
                return hashCode;
            }
        }

        public Event Clone()
        {
            return (Event) MemberwiseClone();
        }
    }
}