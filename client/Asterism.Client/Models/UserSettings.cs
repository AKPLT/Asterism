namespace Asterism.Client.Models;

public sealed class UserSettings
{
    public string ToolsDirectory { get; set; } = "";
    public List<string> FavoriteIds { get; set; } = new();
}
