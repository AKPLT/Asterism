using System.IO;
using ToolPortal.Client.Models;
using ToolPortal.Shared.Models;

namespace ToolPortal.Client.Services;

public sealed class UninstallService : IUninstallService
{
    private readonly ILocalStateService _localStateService;

    public UninstallService(ILocalStateService localStateService)
    {
        _localStateService = localStateService;
    }

    public void Uninstall(ToolEntry tool, InstalledToolRecord record)
    {
        if (Directory.Exists(record.InstallPath))
        {
            Directory.Delete(record.InstallPath, recursive: true);
        }

        _localStateService.Remove(tool.Id);
    }
}
