using ToolPortal.Shared.Models;

namespace ToolPortal.Admin.Services;

public interface IAdminApiService
{
    Task<List<ToolEntry>> GetToolsAsync(CancellationToken ct = default);
    Task<ToolEntry> CreateToolAsync(ToolEntry metadata, string packageFilePath, CancellationToken ct = default);
    Task<ToolEntry> UpdateToolAsync(string id, ToolEntry metadata, string? packageFilePath, CancellationToken ct = default);
    Task DeleteToolAsync(string id, bool purge = false, CancellationToken ct = default);
}
