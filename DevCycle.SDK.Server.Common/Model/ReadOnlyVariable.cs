using System;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;

namespace DevCycle.SDK.Server.Common.Model
{
    [DataContract]
    public class ReadOnlyVariable<T>
    {
        /// <summary>
        /// Variable's unique database ID
        /// </summary>
        /// <value>Unique ID</value>
        [DataMember(Name = "_id")]
        [JsonProperty("_id")]
        public string Id { get; set; }

        /// <summary>
        /// Variable value can be a string, number, boolean, or JSON
        /// </summary>
        /// <value>Variable value can be a string, number, boolean, or JSON</value>
        [DataMember(Name = "value")]
        [JsonProperty("value")]
        public T Value { get; set; }

        /// <summary>
        /// Variable type
        /// </summary>
        /// <value>Variable type</value>
        [DataMember(Name = "type")]
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Unique key by Project, can be used in the SDK / API to reference by &#x27;key&#x27; rather than _id.
        /// </summary>
        /// <value>Unique key by Project, can be used in the SDK / API to reference by &#x27;key&#x27; rather than _id.</value>
        [DataMember(Name = "key")]
        [JsonProperty("key")]
        public string Key { get; set; }

        [Obsolete("use Eval")]
        public string EvalReason { get; set; }

        /// <summary>
        /// Evaluation details
        /// </summary>
        /// <value>Evaluation details</value>
        [DataMember(Name = "eval", EmitDefaultValue = false)]
        [JsonProperty("eval")]
        public EvalReason Eval { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class ReadOnlyVariable {\n");
            sb.Append("  Id: ").Append(Id).Append("\n");
            sb.Append("  Key: ").Append(Key).Append("\n");
            sb.Append("  Type: ").Append(Type).Append("\n");
            sb.Append("  Value: ").Append(Value).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }
    }
}
