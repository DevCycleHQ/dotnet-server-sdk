using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace DevCycle.SDK.Server.Common.Model.Local
{
    public class OptInSettings
    {
        [DataMember(Name = "enabled", EmitDefaultValue = false)]
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [DataMember(Name = "title", EmitDefaultValue = false, IsRequired = false)]
        [JsonProperty("title")]
        public string Title { get; set; }

        [DataMember(Name = "description", EmitDefaultValue = false, IsRequired = false)]
        [JsonProperty("description")]
        public string Description { get; set; }
        
        [DataMember(Name = "imageURL", EmitDefaultValue = false, IsRequired = false)]
        [JsonProperty("imageURL")]
        public string ImageURL { get; set; }
    }
}