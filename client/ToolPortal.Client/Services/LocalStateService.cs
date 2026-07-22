using System.IO;
using System.Text.Json;
using ToolPortal.Client.Models;

namespace ToolPortal.Client.Services;

public sealed class LocalStateService : ILocalStateService
{
    private readonly IUserSettingsService _userSettingsService;
    private readonly string _rootDirectory;
    private readonly string _stateFilePath;
    private readonly object _lock = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public string ToolsRootDirectory =>
        _userSettingsService.ToolsDirectory is { Length: > 0 } dir
            ? dir
            : Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads", "ToolPortal");

    public LocalStateService(IUserSettingsService userSettingsService)
    {
        _userSettingsService = userSettingsService;
        _rootDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ToolPortal");
        _stateFilePath = Path.Combine(_rootDirectory, "installed.json");

        Directory.CreateDirectory(_rootDirectory);
        Directory.CreateDirectory(ToolsRootDirectory);
    }

    public InstalledState Load()
    {
        lock (_lock)
        {
            if (!File.Exists(_stateFilePath))
            {
                return new InstalledState();
            }

            try
            {
                var json = File.ReadAllText(_stateFilePath);
                return JsonSerializer.Deserialize<InstalledState>(json, JsonOptions) ?? new InstalledState();
            }
            catch (Exception ex) when (ex is IOException or JsonException)
            {
                var backupPath = $"{_stateFilePath}.bak.{DateTimeOffset.Now:yyyyMMddHHmmss}";
                try
                {
                    File.Move(_stateFilePath, backupPath, overwrite: true);
                }
                catch (IOException)
                {
                    // バックアップ移動に失敗しても、空状態で継続する
                }

                return new InstalledState();
            }
        }
    }

    public void Save(InstalledState state)
    {
        lock (_lock)
        {
            var json = JsonSerializer.Serialize(state, JsonOptions);
            File.WriteAllText(_stateFilePath, json);
        }
    }

    public InstalledToolRecord? GetRecord(string toolId)
    {
        var state = Load();
        return state.Tools.FirstOrDefault(t => t.Id == toolId);
    }

    public void Upsert(InstalledToolRecord record)
    {
        lock (_lock)
        {
            var state = Load();
            state.Tools.RemoveAll(t => t.Id == record.Id);
            state.Tools.Add(record);
            Save(state);
        }
    }

    public void Remove(string toolId)
    {
        lock (_lock)
        {
            var state = Load();
            state.Tools.RemoveAll(t => t.Id == toolId);
            Save(state);
        }
    }
}
