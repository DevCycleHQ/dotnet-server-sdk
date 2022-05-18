using System.Collections.Generic;

namespace DevCycle.SDK.Server.Common.Model.Local
{
    public class VariableCollection
    {
        private readonly Dictionary<string, Variable<object>> variables;

        public VariableCollection(Dictionary<string, Variable<object>> variables)
        {
            this.variables = variables;
        }

        public bool ContainsKey(string key)
        {
            return variables.ContainsKey(key);
        }

        /// <summary>
        /// Retrieve the Variable by key from the collection as the correct typeof T.
        /// Throws InvalidCastException if the requested conversion is incorrect
        /// </summary>
        public Variable<T> Get<T>(string key)
        {
            Variable<T> existingVariable = null;
            
            var variable = variables[key];
            
            if (variable != null)
            {
                existingVariable = variable.Convert<T>();
            }

            return existingVariable;
        }

        public Dictionary<string, Variable<object>> GetAll()
        {
            return variables;
        }
    }
}