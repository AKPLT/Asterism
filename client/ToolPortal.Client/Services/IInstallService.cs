using ToolPortal.Client.Models;
using ToolPortal.Shared.Models;

namespace ToolPortal.Client.Services;

public interface IInstallService
{
    Task<InstalledToolRecord> InstallOrUpdateAsync(
        ToolEntry tool, IProgress<double> progress, CancellationToken ct = default);
}
