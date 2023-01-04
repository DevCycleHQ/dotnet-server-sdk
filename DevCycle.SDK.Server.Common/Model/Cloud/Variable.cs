using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;

[assembly: InternalsVisibleTo("DevCycle.SDK.Server.Cloud.MSTests")]
[assembly: InternalsVisibleTo( "DevCycle.SDK.Server.Cloud")]

namespace DevCycle.SDK.Server.Common.Model.Cloud
{
 [DataContract]
    public class Variable<T> : IEquatable<Variable<T>>, IVariable
    {
        /// <summary>
        /// Unique key by Project, can be used in the SDK / API to reference by &#x27;key&#x27; rather than _id.
        /// </summary>
        /// <value>Unique key by Project, can be used in the SDK / API to reference by &#x27;key&#x27; rather than _id.</value>
        [DataMember(Name="key")]
        [JsonProperty(Required = Required.Always)]
        public string Key { get; set; }


        /// <summary>
        /// Variable value can be a string, number, boolean, or JSON
        /// </summary>
        /// <value>Variable value can be a string, number, boolean, or JSON</value>
        [DataMember(Name="value")]
        [JsonProperty(Required = Required.Always)]
        public T Value { get; set; }
        
        [DataMember(Name="defaultValue")]
        public T DefaultValue { get; set; }
        
        [DataMember(Name="isDefaulted")]
        public bool IsDefaulted { get; set; }
        
        public string EvalReason { get; set; }
        
        /// <summary>
        /// Variable type
        /// </summary>
        /// <value>Variable type</value>
        [DataMember(Name="type")]
        public TypeEnum Type { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="Variable" /> class.
        /// </summary>
        /// <param name="key">Unique key by Project, can be used in the SDK / API to reference by &#x27;key&#x27; rather than _id. (required).</param>
        /// <param name="type">Variable type (required).</param>
        /// <param name="value">Variable value can be a string, number, boolean, or JSON (required).</param>
        internal Variable(string key = default, T value = default, T defaultValue = default)
        {
            // to ensure "key" is required (not null)
            if (key == null)
            {
                throw new InvalidDataException("key is a required property for Variable and cannot be null");
            }

            Key = key;

            Type = Local.Variable<T>.DetermineType(defaultValue);

            // to ensure "value" is required (not null)
            if (value == null)
            {
                throw new InvalidDataException("value is a required property for Variable and cannot be null");
            }

            if (defaultValue == null)
            {
                throw new InvalidDataException("defaultValue is a required property for Variable and cannot be null");
            } ;

            DefaultValue = defaultValue;

            Value = value;

            IsDefaulted = false;
        }

        public Variable(string key, T defaultValue)
        {
            Key = key;
            Value = defaultValue;
            DefaultValue = defaultValue;
            Type = Local.Variable<T>.DetermineType(defaultValue);
            IsDefaulted = true;
        }

        [JsonConstructor]
        Variable()
        {

        }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class Variable {\n");
            sb.Append("  Key: ").Append(Key).Append("\n");
            sb.Append("  Type: ").Append(Type).Append("\n");
            sb.Append("  Value: ").Append(Value).Append("\n");
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
            return Equals(input as Variable<T>);
        }

        /// <summary>
        /// Returns true if Variable instances are equal
        /// </summary>
        /// <param name="input">Instance of Variable to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(Variable<T> input)
        {
            if (input == null)
                return false;

            return 
                (
                    Key == input.Key ||
                    (Key != null &&
                    Key.Equals(input.Key))
                ) && 
                (
                    Type == input.Type ||
                    Type.Equals(input.Type)
                ) && 
                (
                    (Value != null &&
                    Value.Equals(input.Value))
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
                if (Key != null)
                    hashCode = hashCode * 59 + Key.GetHashCode();
                hashCode = hashCode * 59 + Type.GetHashCode();
                if (Value != null)
                    hashCode = hashCode * 59 + Value.GetHashCode();
                return hashCode;
            }
        }
    }
    
    public static class VariableHelper
    {
        public static Variable<T> Convert<T>(this Variable<object> variable)
        {
            var defaultValue = variable.DefaultValue;
            var value = variable.Value;

            return new Variable<T>(variable.Key, (T) value, (T) defaultValue)
            {
                IsDefaulted = variable.IsDefaulted,
            };
        }
    }
}