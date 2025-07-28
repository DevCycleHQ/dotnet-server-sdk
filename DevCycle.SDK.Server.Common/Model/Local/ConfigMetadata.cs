using System.Runtime.Serialization;

namespace DevCycle.SDK.Server.Common.Model.Local
{
    [DataContract]
    public class ConfigMetadata
    {
        [DataMember(Name = "configETag", EmitDefaultValue = false)]
        public string ConfigETag { get; set; }

        [DataMember(Name = "configLastModified", EmitDefaultValue = false)]
        public string ConfigLastModified { get; set; }

        [DataMember(Name = "project", EmitDefaultValue = false)]
        public ProjectMetadata Project { get; set; }

        [DataMember(Name = "environment", EmitDefaultValue = false)]
        public EnvironmentMetadata Environment { get; set; }
    }
}