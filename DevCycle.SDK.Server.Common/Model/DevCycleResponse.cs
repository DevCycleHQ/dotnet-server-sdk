using System;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;

namespace DevCycle.SDK.Server.Common.Model
{
    
    [DataContract]
    public partial class DevCycleResponse : IEquatable<DevCycleResponse>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DevCycleResponse" /> class.
        /// </summary>
        /// <param name="message">message.</param>
        public DevCycleResponse(string message = default(string))
        {
            this.Message = message;
        }
        
        /// <summary>
        /// Gets or Sets Message
        /// </summary>
        [DataMember(Name="message", EmitDefaultValue=false)]
        public string Message { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class DevCycleResponse {\n");
            sb.Append("  Message: ").Append(Message).Append("\n");
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
            return this.Equals(input as DevCycleResponse);
        }

        /// <summary>
        /// Returns true if InlineResponse201 instances are equal
        /// </summary>
        /// <param name="input">Instance of InlineResponse201 to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(DevCycleResponse input)
        {
            if (input == null)
                return false;

            return 
                (
                    this.Message == input.Message ||
                    (this.Message != null &&
                    this.Message.Equals(input.Message))
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
                if (this.Message != null)
                    hashCode = hashCode * 59 + this.Message.GetHashCode();
                return hashCode;
            }
        }
    }
}