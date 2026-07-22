using System.IO;
using System.Text.Json;
using Asterism.Client.Models;
using Asterism.Client.Options;
using Microsoft.Extensions.Options;

namespace Asterism.Client.Services;

public sealed class UserSettingsService : IUserSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _path;
    private readonly string _defaultServerBaseUrl;
    private readonly UserSettings _settings;

    public string ToolsDirectory
    {
        get => _settings.ToolsDirectory;
        set => _settings.ToolsDirectory = value;
    }

    public string ServerBaseUrl
    {
        get => _settings.ServerBaseUrl is { Length: > 0 } url ? url : _defaultServerBaseUrl;
        set => _settings.ServerBaseUrl = value;
    }

    public UserSettingsService(IOptions<AsterismOptions> options)
    {
        _defaultServerBaseUrl = options.Value.ServerBaseUrl;
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

    public bool IsFavorite(string toolId) => _settings.FavoriteIds.Contains(toolId);

    public void ToggleFavorite(string toolId)
    {
        if (_settings.FavoriteIds.Contains(toolId))
            _settings.FavoriteIds.Remove(toolId);
        else
            _settings.FavoriteIds.Add(toolId);
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(_settings, JsonOptions);
        File.WriteAllText(_path, json);
    }
}
