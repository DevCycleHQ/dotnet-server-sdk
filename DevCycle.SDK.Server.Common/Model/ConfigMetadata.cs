using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace DevCycle.SDK.Server.Common.Model
{
    [DataContract]
    public class ConfigMetadata
    {
        [DataMember(Name = "Project", EmitDefaultValue = false)]
        [JsonProperty("project")]
        public ProjectMetadata Project { get; set; }

        [DataMember(Name = "Environment", EmitDefaultValue = false)]
        [JsonProperty("environment")]
        public EnvironmentMetadata Environment { get; set; }
    }

    [DataContract]
    public class EnvironmentMetadata
    {
        [DataMember(Name = "Id", EmitDefaultValue = false)]
        [JsonProperty("id")]
        public string Id { get; set; }

        [DataMember(Name = "Key", EmitDefaultValue = false)]
        [JsonProperty("key")]
        public string Key { get; set; }
    }

        [DataContract]
    public class ProjectMetadata
    {
        [DataMember(Name = "Id", EmitDefaultValue = false)]
        [JsonProperty("id")]
        public string Id { get; set; }

        [DataMember(Name = "Key", EmitDefaultValue = false)]
        [JsonProperty("key")]
        public string Key { get; set; }
     
    }

}