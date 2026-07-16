namespace Asterism.Client.Services;

public interface IUserSettingsService
{
    string ToolsDirectory { get; set; }
    void Save();
}
