using System;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DevCycle.SDK.Server.Common.Model
{
    /// <summary>
    /// Evaluation reasons enum
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum EvalReasons
    {
        [EnumMember(Value = "TARGETING_MATCH")]
        TargetingMatch,

        [EnumMember(Value = "SPLIT")]
        Split,

        [EnumMember(Value = "DEFAULT")]
        Default,

        [EnumMember(Value = "DISABLED")]
        Disabled,

        [EnumMember(Value = "ERROR")]
        Error,

        [EnumMember(Value = "OVERRIDE")]
        Override,

        [EnumMember(Value = "OPT_IN")]
        OptIn
    }


    /// <summary>
    /// Default reason details constants
    /// </summary>
    public static class DefaultReasonDetails
    {
        public const string MissingConfig = "Missing Config";
        public const string MissingVariable = "Missing Variable";
        public const string MissingFeature = "Missing Feature";
        public const string MissingVariation = "Missing Variation";
        public const string MissingVariableForVariation = "Missing Variable for Variation";
        public const string UserNotInRollout = "User Not in Rollout";
        public const string UserNotTargeted = "User Not Targeted";
        public const string InvalidVariableType = "Invalid Variable Type";
        public const string TypeMismatch = "Variable Type Mismatch";
        public const string Unknown = "Unknown";
        public const string Error = "Error";
    }

    [DataContract]
    public class EvalReason
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EvalReason" /> class.
        /// </summary>
        /// <param name="reason">The evaluation reason (required).</param>
        /// <param name="details">Additional details about the evaluation.</param>
        /// <param name="targetId">Target identifier for the evaluation.</param>
        public EvalReason(EvalReasons reason, string details = null, string targetId = null)
        {
            Reason = reason;
            Details = details;
            TargetId = targetId;
        }

        /// <summary>
        /// Constructor for backward compatibility with string-based reason
        /// </summary>
        /// <param name="reason">The evaluation reason as string.</param>
        /// <param name="details">Additional details about the evaluation.</param>
        /// <param name="targetId">Target identifier for the evaluation.</param>
        [JsonConstructor]
        public EvalReason(string reason, string details = null, string targetId = null)
        {
            // Parse string reason to enum, default to Error if not found
            if (Enum.TryParse<EvalReasons>(reason?.Replace("_", ""), true, out var parsedReason))
            {
                Reason = parsedReason;
            }
            else
            {
                Reason = EvalReasons.Error;
            }
            Details = details;
            TargetId = targetId;
        }

        /// <summary>
        /// The evaluation reason
        /// </summary>
        /// <value>The evaluation reason</value>
        [DataMember(Name = "reason", EmitDefaultValue = false)]
        [JsonProperty("reason")]
        public EvalReasons Reason { get; set; }

        /// <summary>
        /// Additional details about the evaluation
        /// </summary>
        /// <value>Additional details about the evaluation</value>
        [DataMember(Name = "details", EmitDefaultValue = false)]
        [JsonProperty("details", NullValueHandling = NullValueHandling.Ignore)]
        public string Details { get; set; }

        /// <summary>
        /// Target identifier for the evaluation
        /// </summary>
        /// <value>Target identifier for the evaluation</value>
        [DataMember(Name = "target_id", EmitDefaultValue = false)]
        [JsonProperty("target_id", NullValueHandling = NullValueHandling.Ignore)]
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