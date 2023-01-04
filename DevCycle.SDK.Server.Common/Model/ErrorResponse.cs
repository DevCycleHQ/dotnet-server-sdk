using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;

namespace DevCycle.SDK.Server.Common.Model
{
    [DataContract]
    public partial class ErrorResponse : IEquatable<ErrorResponse>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorResponse" /> class.
        /// </summary>
        /// <param name="message">Error message (required).</param>
        /// <param name="data">Additional error information detailing the error reasoning.</param>
        public ErrorResponse(string message = default, Object data = default)
        {
            
            this.Message = message ?? "An Unexpected Error Occurred.";
            this.Data = data;
        }
        
        /// <summary>
        /// Error message
        /// </summary>
        /// <value>Error message</value>
        [DataMember(Name="message", EmitDefaultValue=false)]
        public string Message { get; set; }

        /// <summary>
        /// Additional error information detailing the error reasoning
        /// </summary>
        /// <value>Additional error information detailing the error reasoning</value>
        [DataMember(Name="data", EmitDefaultValue=false)]
        public Object Data { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class ErrorResponse {\n");
            sb.Append("  Message: ").Append(Message).Append("\n");
            sb.Append("  Data: ").Append(Data).Append("\n");
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
            return this.Equals(input as ErrorResponse);
        }

        /// <summary>
        /// Returns true if ErrorResponse instances are equal
        /// </summary>
        /// <param name="input">Instance of ErrorResponse to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(ErrorResponse input)
        {
            if (input == null)
                return false;

            return 
                (
                    this.Message == input.Message ||
                    (this.Message != null &&
                    this.Message.Equals(input.Message))
                ) && 
                (
                    this.Data == input.Data ||
                    (this.Data != null &&
                    this.Data.Equals(input.Data))
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
                if (this.Data != null)
                    hashCode = hashCode * 59 + this.Data.GetHashCode();
                return hashCode;
            }
        }
    }
}