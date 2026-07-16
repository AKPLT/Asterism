using System.Diagnostics;
using System.IO;
using Asterism.Client.Models;
using Asterism.Client.Services.Exceptions;
using Asterism.Shared.Models;

namespace Asterism.Client.Services;

public sealed class LaunchService : ILaunchService
{
    private readonly ILocalStateService _localStateService;

    public LaunchService(ILocalStateService localStateService)
    {
        _localStateService = localStateService;
    }

    public void Launch(ToolEntry tool, InstalledToolRecord record)
    {
        var exePath = record.PackageType == PackageType.Zip
            ? Path.Combine(record.InstallPath, tool.ExecutablePath)
            : Environment.ExpandEnvironmentVariables(record.InstallPath);

        if (!File.Exists(exePath))
        {
            _localStateService.Remove(tool.Id);
            throw new LaunchFailedException("実行ファイルが見つかりません。再インストールしてください。");
        }

        var startInfo = new ProcessStartInfo(exePath)
        {
            UseShellExecute = true,
            WorkingDirectory = Path.GetDirectoryName(exePath) ?? ""
        };

        Process.Start(startInfo);
    }
}
