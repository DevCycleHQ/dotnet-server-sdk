using System;
using System.IO;
using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;

namespace DevCycle.SDK.Server.Common.Model.Local
{
    [DataContract]
    public class Variable<T> : IVariable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Variable" /> class.
        /// </summary>
        /// <param name="key">Unique key by Project, can be used in the SDK / API to reference by &#x27;key&#x27; rather than _id. (required).</param>
        /// <param name="type">Variable type (required).</param>
        /// <param name="value">Variable value can be a string, number, boolean, or JSON (required).</param>
        public Variable(string key = default, T value = default, T defaultValue = default)
        {
            // to ensure "key" is required (not null)

            Key = key ?? throw new InvalidDataException("key is a required property for Variable and cannot be null");

            Type = DetermineType(value);

            // to ensure "value" is required (not null)
            Value = value ??
                    throw new InvalidDataException("value is a required property for Variable and cannot be null");

            DefaultValue = defaultValue ??
                           throw new InvalidDataException(
                               "defaultValue is a required property for Variable and cannot be null");

            IsDefaulted = false;
        }

        public Variable(string key, T defaultValue)
        {
            Key = key;
            Value = defaultValue;
            DefaultValue = defaultValue;
            Type = DetermineType(defaultValue);
            IsDefaulted = true;
        }

        public Variable(ReadOnlyVariable<object> readOnlyVariable, T defaultValue)
        {
            Key = readOnlyVariable.Key;
            Value = (T)readOnlyVariable.Value;
            DefaultValue = defaultValue;
            Type = DetermineType(defaultValue);
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
                returnVariable.Key = variable.Key;
                returnVariable.Value = variable.Value;
                returnVariable.DefaultValue = defaultValue;
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
        [DataMember(Name = "value")]
        public T Value { get; set; }

        [DataMember(Name = "defaultValue")] public T DefaultValue { get; set; }

        /// <summary>
        /// Variable type
        /// </summary>
        /// <value>Variable type</value>
        [DataMember(Name = "type")]
        public TypeEnum Type { get; set; }

        /// <summary>
        /// Unique key by Project, can be used in the SDK / API to reference by &#x27;key&#x27; rather than _id.
        /// </summary>
        /// <value>Unique key by Project, can be used in the SDK / API to reference by &#x27;key&#x27; rather than _id.</value>
        [DataMember(Name = "key")]
        public string Key { get; set; }

        [DataMember(Name = "isDefaulted")] public bool IsDefaulted { get; set; }
        public string EvalReason { get; set; }

        public static TypeEnum DetermineType(T variableValue)
        {
            TypeEnum typeEnum;

            try
            {
                var baseType = variableValue.GetType();

                if (baseType == typeof(string))
                {
                    typeEnum = TypeEnum.String;
                }
                else if (IsNumericType(baseType))
                {
                    typeEnum = TypeEnum.Number;
                }
                else if (baseType == typeof(bool))
                {
                    typeEnum = TypeEnum.Boolean;
                }
                else if (baseType.IsSubclassOf(typeof(JContainer)))
                {
                    typeEnum = TypeEnum.JSON;
                }
                else
                {
                    throw new ArgumentException(
                        $"{baseType} is not a valid type. Must be String / Number / Boolean or a subclass of a JObject");
                }

                return typeEnum;
            }
            catch (System.Exception e)
            {
                Console.WriteLine(variableValue);
                Console.WriteLine(e);
                throw;
            }
        }

        private static bool IsNumericType(Type type)
        {
            if (type == null)
            {
                return false;
            }

            switch (System.Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
                case TypeCode.Object:
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        return IsNumericType(Nullable.GetUnderlyingType(type));
                    }

                    return false;
                default: return false;
            }
        }
    }
}