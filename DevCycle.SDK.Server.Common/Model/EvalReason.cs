using System;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;

namespace DevCycle.SDK.Server.Common.Model
{
    [DataContract]
    public class EvalReason : IEquatable<EvalReason>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EvalReason" /> class.
        /// </summary>
        /// <param name="reason">The evaluation reason (required).</param>
        /// <param name="details">Additional details about the evaluation.</param>
        /// <param name="targetId">Target identifier for the evaluation.</param>
        public EvalReason(string reason = default, string details = default, string targetId = default)
        {
            Reason = reason ?? throw new ArgumentException("reason is a required property for EvalReason and cannot be null");
            Details = details;
            TargetId = targetId;
        }

        /// <summary>
        /// The evaluation reason
        /// </summary>
        /// <value>The evaluation reason</value>
        [DataMember(Name = "reason", EmitDefaultValue = false)]
        [JsonProperty("reason")]
        public string Reason { get; set; }

        /// <summary>
        /// Additional details about the evaluation
        /// </summary>
        /// <value>Additional details about the evaluation</value>
        [DataMember(Name = "details", EmitDefaultValue = false)]
        [JsonProperty("details")]
        public string Details { get; set; }

        /// <summary>
        /// Target identifier for the evaluation
        /// </summary>
        /// <value>Target identifier for the evaluation</value>
        [DataMember(Name = "targetId", EmitDefaultValue = false)]
        [JsonProperty("targetId")]
        public string TargetId { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class EvalReason {\n");
            sb.Append("  Reason: ").Append(Reason).Append("\n");
            sb.Append("  Details: ").Append(Details).Append("\n");
            sb.Append("  TargetId: ").Append(TargetId).Append("\n");
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
            return Equals(input as EvalReason);
        }

        /// <summary>
        /// Returns true if EvalReason instances are equal
        /// </summary>
        /// <param name="input">Instance of EvalReason to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(EvalReason input)
        {
            if (input == null)
                return false;

            return
                (
                    Reason == input.Reason ||
                    (Reason != null && Reason.Equals(input.Reason))
                ) &&
                (
                    Details == input.Details ||
                    (Details != null && Details.Equals(input.Details))
                ) &&
                (
                    TargetId == input.TargetId ||
                    (TargetId != null && TargetId.Equals(input.TargetId))
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
                if (Reason != null)
                    hashCode = hashCode * 59 + Reason.GetHashCode();
                if (Details != null)
                    hashCode = hashCode * 59 + Details.GetHashCode();
                if (TargetId != null)
                    hashCode = hashCode * 59 + TargetId.GetHashCode();
                return hashCode;
            }
        }
    }
}