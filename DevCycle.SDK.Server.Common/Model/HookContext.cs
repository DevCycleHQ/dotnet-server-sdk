namespace DevCycle.SDK.Server.Common.Model
{
    public class HookContext<T>
    {
        public DevCycleUser User { get; private set; }
        public string Key { get; private set; }
        public T DefaultValue { get; private set; }
        public Variable<T> VariableDetails { get; private set; }

        public HookContext(DevCycleUser user, string key, T defaultValue, Variable<T> variableDetails)
        {
            this.User = user;
            this.Key = key;
            this.DefaultValue = defaultValue;
            this.VariableDetails = variableDetails;
        }
        public HookContext<T> Merge(HookContext<T> other)
        {
            return new HookContext<T>(other.User ?? this.User, this.Key, this.DefaultValue, this.VariableDetails);
        }
    }
}
