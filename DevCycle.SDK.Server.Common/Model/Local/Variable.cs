using System;
using System.IO;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using TypeSupport.Extensions;

namespace DevCycle.SDK.Server.Common.Model.Local
{
    public static class VariableHelper
    {
        public static Variable<T> Convert<T>(this Variable<object> variable)
        {
            var defaultValue = variable.DefaultValue ?? variable.Value;
            var value = variable.Value;

            return new Variable<T>(variable.Id, variable.Key, variable.Type, (T) value)
            {
                DefaultValue = (T) defaultValue,
                EvalReason = variable.EvalReason,
                IsDefaulted = variable.IsDefaulted,
            };
        }
    }

    [DataContract]
    public class Variable<T> : IVariable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Variable" /> class.
        /// </summary>
        /// <param name="id">unique database id (required).</param>
        /// <param name="key">Unique key by Project, can be used in the SDK / API to reference by &#x27;key&#x27; rather than _id. (required).</param>
        /// <param name="type">Variable type (required).</param>
        /// <param name="value">Variable value can be a string, number, boolean, or JSON (required).</param>
        public Variable(string id = default, string key = default, TypeEnum type = default, T value = default)
        {
            // to ensure "id" is required (not null)

            Id = id ?? throw new InvalidDataException("id is a required property for Variable and cannot be null");
            // to ensure "key" is required (not null)

            Key = key ?? throw new InvalidDataException("key is a required property for Variable and cannot be null");

            Type = type;

            // to ensure "value" is required (not null)
            Value = value ??
                    throw new InvalidDataException("value is a required property for Variable and cannot be null");

            IsDefaulted = false;
        }

        public Variable(string key, T defaultValue, string evalReason)
        {
            Key = key;
            Value = defaultValue;
            EvalReason = evalReason;
            IsDefaulted = true;
        }

        // parameterless private constructor for testing
        private Variable()
        {
        }

        public static Variable<T> InitializeFromVariable(Variable<T> variable, string key, T defaultValue)
        {
            var returnVariable = new Variable<T>();
            if (variable != null)
            {
                returnVariable.Id = variable.Id;
                returnVariable.Key = variable.Key;
                returnVariable.Value = variable.Value;
                returnVariable.Type = variable.Type;
                returnVariable.EvalReason = variable.EvalReason;
                returnVariable.IsDefaulted = false;
            }
            else
            {
                returnVariable.Key = key;
                returnVariable.Value = defaultValue;
                returnVariable.DefaultValue = defaultValue;
                returnVariable.IsDefaulted = true;
                returnVariable.Type = DetermineType(defaultValue);
            }

            return returnVariable;
        }


        /// <summary>
        /// Variable value can be a string, number, boolean, or JSON
        /// </summary>
        /// <value>Variable value can be a string, number, boolean, or JSON</value>
        [DataMember(Name = "value", EmitDefaultValue = false)]
        public T Value { get; set; }

        public T DefaultValue { get; set; }

        /// <summary>
        /// Variable type
        /// </summary>
        /// <value>Variable type</value>
        [DataMember(Name="type", EmitDefaultValue=false)]
        public TypeEnum Type { get; set; }
        /// <summary>
        /// unique database id
        /// </summary>
        /// <value>unique database id</value>
        [DataMember(Name = "_id", EmitDefaultValue = false)]
        public string Id { get; set; }
        
        /// <summary>
        /// Unique key by Project, can be used in the SDK / API to reference by &#x27;key&#x27; rather than _id.
        /// </summary>
        /// <value>Unique key by Project, can be used in the SDK / API to reference by &#x27;key&#x27; rather than _id.</value>
        [DataMember(Name = "key", EmitDefaultValue = false)]
        public string Key { get; set; }
        public bool IsDefaulted { get; set; }
        public string EvalReason { get; set; }

        private static TypeEnum DetermineType(T variableType)
        {
            TypeEnum typeEnum;
            var type = variableType.GetType().GetExtendedType();

            if (type == typeof(string))
            {
                typeEnum = TypeEnum.String;
            }
            else if (type.IsNumericType)
            {
                typeEnum = TypeEnum.Number;
            }
            else if (type == typeof(bool))
            {
                typeEnum = TypeEnum.Boolean;
            }
            else if (variableType.GetType().IsSubclassOf(typeof(JContainer)))
            {
                typeEnum = TypeEnum.JSON;
            }
            else
            {
                throw new ArgumentException(
                    $"{type} is not a valid type. Must be String / Number / Boolean or a subclass of a JContainer (JArray / JObject)");
            }

            return typeEnum;
        }
    }
}