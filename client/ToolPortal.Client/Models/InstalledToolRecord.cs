namespace ToolPortal.Client.Models;

public sealed class InstalledToolRecord
{
    public string Id { get; set; } = "";
    public string Version { get; set; } = "";
    public string InstallPath { get; set; } = "";
    public DateTimeOffset InstalledAt { get; set; }
}
