using System.Runtime.Serialization;

namespace DevCycle.SDK.Server.Common.Model.Local
{
    [DataContract]
    public class Project
    {
        [DataMember(Name = "_id", EmitDefaultValue = false)]
        public string Id { get; set; }

        [DataMember(Name = "a0_organization", EmitDefaultValue = false)]
        public string Organization { get; set; }

        [DataMember(Name = "key", EmitDefaultValue = false)]
        public string Key { get; set; }

        [DataMember(Name = "settings", EmitDefaultValue = false)]
        public ProjectSettings Settings { get; set; }
    }
}