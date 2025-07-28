using DevCycle.SDK.Server.Common.Model.Local;

namespace DevCycle.SDK.Server.Common.Model
{
    public class HookContext<T>
    {
        public DevCycleUser User { get; private set; }
        public string Key { get; private set; }
        public T DefaultValue { get; private set; }
        public Variable<T> VariableDetails { get; private set; }
        public ConfigMetadata Metadata { get; private set; }

        public HookContext(DevCycleUser user, string key, T defaultValue, Variable<T> variableDetails, ConfigMetadata metadata = null)
        {
            this.User = user;
            this.Key = key;
            this.DefaultValue = defaultValue;
            this.VariableDetails = variableDetails;
            this.Metadata = metadata ?? new ConfigMetadata();
        }
        public HookContext<T> Merge(HookContext<T> other)
        {
            return new HookContext<T>(other.User ?? this.User, this.Key, this.DefaultValue, this.VariableDetails, this.Metadata);
        }
    }
}
