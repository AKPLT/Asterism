using System.IO;
using Asterism.Client.Models;

namespace Asterism.Client.Services;

public sealed class UninstallService : IUninstallService
{
    private readonly ILocalStateService _localStateService;

    public UninstallService(ILocalStateService localStateService)
    {
        _localStateService = localStateService;
    }

    public UninstallOutcome Uninstall(ToolEntry tool, InstalledToolRecord record)
    {
        if (record.PackageType == PackageType.Installer)
        {
            // インストール先・アンインストーラーがベンダー任せのため、記録の削除のみ行う
            _localStateService.Remove(tool.Id);
            return UninstallOutcome.ManualActionRequired;
        }

        if (Directory.Exists(record.InstallPath))
        {
            Directory.Delete(record.InstallPath, recursive: true);
        }

        _localStateService.Remove(tool.Id);
        return UninstallOutcome.Removed;
    }
}
