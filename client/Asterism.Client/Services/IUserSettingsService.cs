namespace Asterism.Client.Services;

public interface IUserSettingsService
{
    string ToolsDirectory { get; set; }
    bool IsFavorite(string toolId);
    void ToggleFavorite(string toolId);
    void Save();
}
