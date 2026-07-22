using System.Text.Json.Serialization;

namespace ToolPortal.Shared.Models;

public sealed class ToolManifest
{
    [JsonPropertyName("tools")]
    public List<ToolEntry> Tools { get; set; } = new();
}
