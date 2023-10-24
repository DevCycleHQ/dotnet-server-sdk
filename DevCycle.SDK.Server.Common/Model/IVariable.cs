using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DevCycle.SDK.Server.Common.Model
{
    /// <summary>
    /// Variable type according to DevCycle.
    /// </summary>
    /// <value>Variable type</value>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TypeEnum
    {
        /// <summary>
        /// Enum String for value: String
        /// </summary>
        [EnumMember(Value = "String")] String = 1,

        /// <summary>
        /// Enum Boolean for value: Boolean
        /// </summary>
        [EnumMember(Value = "Boolean")] Boolean = 2,

        /// <summary>
        /// Enum Number for value: Number
        /// </summary>
        [EnumMember(Value = "Number")] Number = 3,

        /// <summary>
        /// Enum JSON for value: JSON
        /// </summary>
        [EnumMember(Value = "JSON")] JSON = 4
    }

    public interface IVariable
    {
        /// <summary>
        /// Variable type
        /// </summary>
        /// <value>Variable type</value>
        [DataMember(Name = "type")]
        public TypeEnum Type { get; set; }

        public string Key { get; set; }
        
        public bool IsDefaulted { get; set; }

        public string EvalReason { get; set; }

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