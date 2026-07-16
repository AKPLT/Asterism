using Asterism.Shared.Models;

namespace Asterism.Client.Services;

public interface IAdminApiService
{
    bool IsUnlocked { get; }

    Task<bool> TryUnlockAsync(string adminKey, CancellationToken ct = default);
    void Lock();

    Task<List<ToolEntry>> GetToolsAsync(CancellationToken ct = default);
    Task<ToolEntry> CreateToolAsync(ToolEntry metadata, string packageFilePath, string? iconFilePath, CancellationToken ct = default);
    Task<ToolEntry> UpdateToolAsync(string id, ToolEntry metadata, string? packageFilePath, string? iconFilePath, CancellationToken ct = default);
    Task DeleteToolAsync(string id, CancellationToken ct = default);
}
