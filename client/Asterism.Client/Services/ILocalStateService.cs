using Asterism.Client.Models;

namespace Asterism.Client.Services;

public interface ILocalStateService
{
    string ToolsRootDirectory { get; }

    InstalledState Load();
    void Save(InstalledState state);
    InstalledToolRecord? GetRecord(string toolId);
    void Upsert(InstalledToolRecord record);
    void Remove(string toolId);
}
