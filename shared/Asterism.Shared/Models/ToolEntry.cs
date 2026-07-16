namespace Asterism.Shared.Models;

public sealed class ToolEntry
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public List<string> Tags { get; set; } = new();
    public string IconUrl { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public PackageType PackageType { get; set; }
    public string ExecutablePath { get; set; } = "";
    public bool IsDisabled { get; set; } = false;
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
