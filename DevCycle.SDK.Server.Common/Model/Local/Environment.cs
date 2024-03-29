using System.Runtime.Serialization;

namespace DevCycle.SDK.Server.Common.Model.Local
{
    [DataContract]
    public class Environment
    {
        [DataMember(Name="_id", EmitDefaultValue=false)]
        public string Id { get; set; }

        [DataMember(Name = "key", EmitDefaultValue = false)]
        public string Key { get; set; }
    }
}