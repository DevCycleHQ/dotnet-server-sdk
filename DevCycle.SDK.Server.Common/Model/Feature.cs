using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DevCycle.SDK.Server.Common.Model
{
    [DataContract]
    public partial class Feature : IEquatable<Feature>
    {
        /// <summary>
        /// Feature type
        /// </summary>
        /// <value>Feature type</value>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum TypeEnum
        {
            /// <summary>
            /// Enum Release for value: release
            /// </summary>
            [EnumMember(Value = "release")]
            Release = 1,
            /// <summary>
            /// Enum Experiment for value: experiment
            /// </summary>
            [EnumMember(Value = "experiment")]
            Experiment = 2,
            /// <summary>
            /// Enum Permission for value: permission
            /// </summary>
            [EnumMember(Value = "permission")]
            Permission = 3,
            /// <summary>
            /// Enum Ops for value: ops
            /// </summary>
            [EnumMember(Value = "ops")]
            Ops = 4
        }

        /// <summary>
        /// Feature type
        /// </summary>
        /// <value>Feature type</value>
        [DataMember(Name="type", EmitDefaultValue=false)]
        public TypeEnum Type { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="Feature" /> class.
        /// </summary>
        /// <param name="id">unique database id (required).</param>
        /// <param name="key">Unique key by Project, can be used in the SDK / API to reference by &#x27;key&#x27; rather than _id. (required).</param>
        /// <param name="type">Feature type (required).</param>
        /// <param name="variation">Bucketed feature variation ID (required).</param>
        /// <param name="variationKey">Bucketed feature variation key (required).</param>
        /// <param name="variationName">Bucketed feature variation name (required).</param>
        /// <param name="evalReason">Evaluation reasoning.</param>
        public Feature(string id = default, string key = default, TypeEnum type = default, string variation = default, string variationKey = default, string variationName = default, string evalReason = default)
        {
            // to ensure "id" is required (not null)
            if (id == null)
            {
                throw new InvalidDataException("id is a required property for Feature and cannot be null");
            }
            else
            {
                this.Id = id;
            }
            // to ensure "key" is required (not null)
            if (key == null)
            {
                throw new InvalidDataException("key is a required property for Feature and cannot be null");
            }
            else
            {
                this.Key = key;
            }

            this.Type = type;

            // to ensure "variation" is required (not null)
            if (variation == null)
            {
                throw new InvalidDataException("variation is a required property for Feature and cannot be null");
            }
            else
            {
                this.Variation = variation;
            }

            // to ensure "variationKey" is required (not null)
            if (variationKey == null)
            {
                throw new InvalidDataException("variationKey is a required property for Feature and cannot be null");
            }
            else
            {
                this.VariationKey = variationKey;
            }

            // to ensure "variationName" is required (not null)
            if (variationName == null)
            {
                throw new InvalidDataException("variationName is a required property for Feature and cannot be null");
            }
            else
            {
                this.VariationName = variationName;
            }
            this.EvalReason = evalReason;
        }
        
        /// <summary>
        /// unique database id
        /// </summary>
        /// <value>unique database id</value>
        [DataMember(Name="_id", EmitDefaultValue=false)]
        public string Id { get; set; }

        /// <summary>
        /// Unique key by Project, can be used in the SDK / API to reference by &#x27;key&#x27; rather than _id.
        /// </summary>
        /// <value>Unique key by Project, can be used in the SDK / API to reference by &#x27;key&#x27; rather than _id.</value>
        [DataMember(Name="key", EmitDefaultValue=false)]
        public string Key { get; set; }


        /// <summary>
        /// Bucketed feature variation ID
        /// </summary>
        /// <value>Bucketed feature variation</value>
        [DataMember(Name="_variation", EmitDefaultValue=false)]
        public string Variation { get; set; }

        /// <summary>
        /// Bucketed feature variation key
        /// </summary>
        /// <value>Bucketed feature variation key</value>
        [DataMember(Name="variationKey", EmitDefaultValue=false)]
        public string VariationKey { get; set; }

        /// <summary>
        /// Bucketed feature variation name
        /// </summary>
        /// <value>Bucketed feature variation name</value>
        [DataMember(Name="variationName", EmitDefaultValue=false)]
        public string VariationName { get; set; }


        /// <summary>
        /// Evaluation reasoning
        /// </summary>
        /// <value>Evaluation reasoning</value>
        [DataMember(Name="evalReason", EmitDefaultValue=false)]
        public string EvalReason { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class Feature {\n");
            sb.Append("  Id: ").Append(Id).Append("\n");
            sb.Append("  Key: ").Append(Key).Append("\n");
            sb.Append("  Type: ").Append(Type).Append("\n");
            sb.Append("  Variation: ").Append(Variation).Append("\n");
            sb.Append("  VariationKey: ").Append(VariationKey).Append("\n");
            sb.Append("  VariationName: ").Append(VariationName).Append("\n");
            sb.Append("  EvalReason: ").Append(EvalReason).Append("\n");
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
            return this.Equals(input as Feature);
        }

        /// <summary>
        /// Returns true if Feature instances are equal
        /// </summary>
        /// <param name="input">Instance of Feature to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(Feature input)
        {
            if (input == null)
            {
                return false;
            }

            return 
                (
                    this.Id == input.Id ||
                    (this.Id != null &&
                    this.Id.Equals(input.Id))
                ) && 
                (
                    this.Key == input.Key ||
                    (this.Key != null &&
                    this.Key.Equals(input.Key))
                ) && 
                (
                    this.Type == input.Type ||
                    this.Type.Equals(input.Type)
                ) && 
                (
                    this.Variation == input.Variation ||
                    (this.Variation != null &&
                    this.Variation.Equals(input.Variation))
                ) && 
                (
                    this.VariationKey == input.VariationKey ||
                    (this.VariationKey != null &&
                    this.VariationKey.Equals(input.VariationKey))
                ) && 
                (
                    this.VariationName == input.VariationName ||
                    (this.VariationName != null &&
                    this.VariationName.Equals(input.VariationName))
                ) && 
                (
                    this.EvalReason == input.EvalReason ||
                    (this.EvalReason != null &&
                    this.EvalReason.Equals(input.EvalReason))
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
                if (this.Id != null)
                {
                    hashCode = hashCode * 59 + this.Id.GetHashCode();
                }
                if (this.Key != null)
                {
                    hashCode = hashCode * 59 + this.Key.GetHashCode();
                }

                hashCode = hashCode * 59 + this.Type.GetHashCode();

                if (this.Variation != null)
                {
                    hashCode = hashCode * 59 + this.Variation.GetHashCode();
                }

                if (this.VariationKey != null)
                {
                    hashCode = hashCode * 59 + this.VariationKey.GetHashCode();
                }

                if (this.VariationName != null)
                {
                    hashCode = hashCode * 59 + this.VariationName.GetHashCode();
                }
                
                if (this.EvalReason != null)
                {
                    hashCode = hashCode * 59 + this.EvalReason.GetHashCode();
                }
                return hashCode;
            }
        }
    }
}