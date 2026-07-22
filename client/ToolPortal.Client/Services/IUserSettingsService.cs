namespace ToolPortal.Client.Services;

public interface IUserSettingsService
{
    string ToolsDirectory { get; set; }
    string ServerBaseUrl { get; set; }
    bool IsFavorite(string toolId);
    void ToggleFavorite(string toolId);
    void Save();
}
