namespace Asterism.Client.Models;

public sealed class InstalledState
{
    public List<InstalledToolRecord> Tools { get; set; } = new();
}
