using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using Asterism.Client.Models;
using Asterism.Client.Services.Exceptions;
using Asterism.Shared.Models;

namespace Asterism.Client.Services;

public sealed class InstallService : IInstallService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILocalStateService _localStateService;
    private readonly string _downloadsDirectory;

    public InstallService(IHttpClientFactory httpClientFactory, ILocalStateService localStateService)
    {
        _httpClientFactory = httpClientFactory;
        _localStateService = localStateService;

        _downloadsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Asterism", "Downloads");
        Directory.CreateDirectory(_downloadsDirectory);
    }

    public async Task<InstalledToolRecord> InstallOrUpdateAsync(
        ToolEntry tool, IProgress<double> progress, CancellationToken ct = default)
    {
        // Installer型はUseShellExecute=trueで実行するため、シェルが実行方式を判別できるよう
        // 拡張子(.exe/.msi等)を維持したファイル名でダウンロードする（.tmpのままだと起動に失敗する）。
        var tempExtension = tool.PackageType == PackageType.Installer
            ? (Path.GetExtension(tool.DownloadUrl) is { Length: > 0 } ext ? ext : ".exe")
            : ".tmp";
        var tempFilePath = Path.Combine(_downloadsDirectory, $"{tool.Id}-{tool.Version}{tempExtension}");

        try
        {
            await DownloadAsync(tool.DownloadUrl, tempFilePath, progress, ct);

            InstalledToolRecord record = tool.PackageType switch
            {
                PackageType.Zip => InstallZip(tool, tempFilePath),
                PackageType.Installer => await RunInstallerAsync(tool, tempFilePath, ct),
                _ => throw new NotSupportedException($"未対応のpackageTypeです: {tool.PackageType}")
            };

            _localStateService.Upsert(record);
            return record;
        }
        finally
        {
            try
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
            catch (IOException)
            {
                // 一時ファイル削除の失敗は致命的ではないため無視する
            }
        }
    }

    private async Task DownloadAsync(string downloadUrl, string destinationPath, IProgress<double> progress, CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("AsterismServer");
            using var response = await client.GetAsync(
                new Uri(downloadUrl, UriKind.RelativeOrAbsolute),
                HttpCompletionOption.ResponseHeadersRead,
                ct);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength;
            await using var httpStream = await response.Content.ReadAsStreamAsync(ct);
            await using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);

            var buffer = new byte[81920];
            long readTotal = 0;
            int read;
            while ((read = await httpStream.ReadAsync(buffer, ct)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, read), ct);
                readTotal += read;
                if (totalBytes is > 0)
                {
                    progress.Report((double)readTotal / totalBytes.Value);
                }
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new PackageCorruptException("ダウンロードに失敗しました。", ex);
        }
    }

    private InstalledToolRecord InstallZip(ToolEntry tool, string zipFilePath)
    {
        var finalDir = Path.Combine(_localStateService.ToolsRootDirectory, tool.Id);
        var stagingRoot = Path.Combine(_localStateService.ToolsRootDirectory, ".staging");
        Directory.CreateDirectory(stagingRoot);
        var stagingDir = Path.Combine(stagingRoot, $"{tool.Id}-{tool.Version}");

        if (Directory.Exists(stagingDir))
        {
            Directory.Delete(stagingDir, recursive: true);
        }

        try
        {
            ZipFile.ExtractToDirectory(zipFilePath, stagingDir);
        }
        catch (InvalidDataException ex)
        {
            if (Directory.Exists(stagingDir))
            {
                Directory.Delete(stagingDir, recursive: true);
            }
            throw new PackageCorruptException("パッケージが破損しています。", ex);
        }

        // 展開成功後にのみ既存インストールを置き換える（更新失敗時に既存動作を壊さないため）
        if (Directory.Exists(finalDir))
        {
            Directory.Delete(finalDir, recursive: true);
        }
        Directory.Move(stagingDir, finalDir);

        return new InstalledToolRecord
        {
            Id = tool.Id,
            Version = tool.Version,
            PackageType = PackageType.Zip,
            InstallPath = finalDir,
            InstalledAt = DateTimeOffset.Now
        };
    }

    private static async Task<InstalledToolRecord> RunInstallerAsync(ToolEntry tool, string installerFilePath, CancellationToken ct)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = installerFilePath,
                Arguments = tool.InstallerArgs ?? "",
                UseShellExecute = true
            };

            using var process = Process.Start(startInfo)
                ?? throw new InstallerFailedException("インストーラーの起動に失敗しました。");

            await process.WaitForExitAsync(ct);

            if (process.ExitCode != 0)
            {
                throw new InstallerFailedException(
                    $"インストーラーが正常終了しませんでした（終了コード: {process.ExitCode}）。", process.ExitCode);
            }
        }
        catch (Win32Exception ex)
        {
            throw new InstallerFailedException("インストーラーの実行が許可されませんでした。", inner: ex);
        }

        return new InstalledToolRecord
        {
            Id = tool.Id,
            Version = tool.Version,
            PackageType = PackageType.Installer,
            InstallPath = Environment.ExpandEnvironmentVariables(tool.ExecutablePath),
            InstalledAt = DateTimeOffset.Now
        };
    }
}
