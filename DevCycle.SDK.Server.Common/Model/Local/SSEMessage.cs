using System.Text.Json;
using System.Text.Json.Serialization;

namespace DevCycle.SDK.Server.Common.Model.Local;

/// <summary>
/// Outer SSE event envelope. The <c>data</c> field is a JSON-encoded string
/// whose contents deserialize into <see cref="SSEMessage"/>.
/// This is required since the newer LD EventSource clients return the full object; not just the `data` field.
/// </summary>
public class SSEEvent
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("channel")]
    public string Channel { get; set; }
    
    [JsonPropertyName("action")]
    public int Action { get; set; }
    
    [JsonPropertyName("serial")]
    public string Serial { get; set; }
    
    /// <summary>
    /// JSON-encoded string containing an <see cref="SSEMessage"/> payload.
    /// </summary>
    [JsonPropertyName("data")]
    public string Data { get; set; }
    
    /// <summary>
    /// Deserializes the inner <see cref="Data"/> JSON string into an <see cref="SSEMessage"/>.
    /// Returns null if <see cref="Data"/> is null or cannot be parsed.
    /// </summary>
    public SSEMessage GetMessageData()
    {
        if (string.IsNullOrEmpty(Data)) return null;
        return JsonSerializer.Deserialize<SSEMessage>(Data);
    }
    
    public SSEEvent(){}
}

/// <summary>
/// Inner data payload containing config change metadata.
/// </summary>
public class SSEMessage
{
    [JsonPropertyName("etag")]
    public string Etag { get; set; }
    [JsonPropertyName("type")]
    public string Type { get; set; }
    [JsonPropertyName("lastModified")]
    public long LastModified { get; set; }
    
    public SSEMessage(){}
}