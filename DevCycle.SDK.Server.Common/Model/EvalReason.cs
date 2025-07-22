using System;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;

namespace DevCycle.SDK.Server.Common.Model
{
    [DataContract]
    public class EvalReason
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EvalReason" /> class.
        /// </summary>
        /// <param name="reason">The evaluation reason (required).</param>
        /// <param name="details">Additional details about the evaluation.</param>
        /// <param name="targetId">Target identifier for the evaluation.</param>
        public EvalReason(string reason = default, string details = default, string targetId = default)
        {
            Reason = reason;
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
        [DataMember(Name = "target_id", EmitDefaultValue = false)]
        [JsonProperty("target_id")]
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

    }
}