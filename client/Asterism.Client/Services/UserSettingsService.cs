using System.IO;
using System.Text.Json;
using Asterism.Client.Models;

namespace Asterism.Client.Services;

public sealed class UserSettingsService : IUserSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _path;
    private readonly UserSettings _settings;

    public string ToolsDirectory
    {
        get => _settings.ToolsDirectory;
        set => _settings.ToolsDirectory = value;
    }

    public UserSettingsService()
    {
        _path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Asterism", "user-settings.json");
        _settings = Load();
    }

    private UserSettings Load()
    {
        if (!File.Exists(_path)) return new UserSettings();
        try
        {
            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<UserSettings>(json, JsonOptions) ?? new UserSettings();
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            return new UserSettings();
        }
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(_settings, JsonOptions);
        File.WriteAllText(_path, json);
    }
}
