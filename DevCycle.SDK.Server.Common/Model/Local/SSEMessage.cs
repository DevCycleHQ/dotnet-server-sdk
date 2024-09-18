using System.Numerics;
using System.Text.Json.Serialization;

namespace DevCycle.SDK.Server.Common.Model.Local;

public class SSEMessage
{
    [JsonPropertyName("etag")]
    public string Etag { get; set; }
    [JsonPropertyName("type")]
    public string Type { get; set; }
    [JsonPropertyName("lastModified")]
    public uint LastModified { get; set; }
    
    public SSEMessage(){}
}