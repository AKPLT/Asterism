using System.Diagnostics;
using System.IO;
using ToolPortal.Client.Models;
using ToolPortal.Client.Services.Exceptions;
using ToolPortal.Shared.Models;

namespace ToolPortal.Client.Services;

public sealed class LaunchService : ILaunchService
{
    private readonly ILocalStateService _localStateService;

    public LaunchService(ILocalStateService localStateService)
    {
        _localStateService = localStateService;
    }

    public void Launch(ToolEntry tool, InstalledToolRecord record)
    {
        var exePath = Path.Combine(record.InstallPath, tool.ExecutablePath);

        if (!File.Exists(exePath))
        {
            _localStateService.Remove(tool.Id);
            throw new LaunchFailedException("実行ファイルが見つかりません。再インストールしてください。");
        }

        Process.Start(new ProcessStartInfo(exePath)
        {
            UseShellExecute = true,
            WorkingDirectory = Path.GetDirectoryName(exePath) ?? ""
        });
    }
}
