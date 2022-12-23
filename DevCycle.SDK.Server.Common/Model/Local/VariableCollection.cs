using System.Collections.Generic;

namespace DevCycle.SDK.Server.Common.Model.Local
{
    public class VariableCollection
    {
        private readonly Dictionary<string, ReadOnlyVariable<object>> variables;

        public VariableCollection(Dictionary<string, ReadOnlyVariable<object>> variables)
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
        public ReadOnlyVariable<object> Get(string key)
        {
            if (!variables.ContainsKey(key))
                throw new KeyNotFoundException(key);
            
            var variable = variables[key];
            return variables[key];
        }

        public Dictionary<string, ReadOnlyVariable<object>> GetAll()
        {
            return variables;
        }
    }
}