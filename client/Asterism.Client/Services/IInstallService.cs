using Asterism.Client.Models;

namespace Asterism.Client.Services;

public interface IInstallService
{
    Task<InstalledToolRecord> InstallOrUpdateAsync(
        ToolEntry tool, IProgress<double> progress, CancellationToken ct = default);
}
