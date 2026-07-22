using System.IO;
using System.IO.Compression;
using System.Net.Http;
using ToolPortal.Client.Models;
using ToolPortal.Client.Services.Exceptions;
using ToolPortal.Shared.Models;

namespace ToolPortal.Client.Services;

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
            "ToolPortal", "Downloads");
        Directory.CreateDirectory(_downloadsDirectory);
    }

    public async Task<InstalledToolRecord> InstallOrUpdateAsync(
        ToolEntry tool, IProgress<double> progress, CancellationToken ct = default)
    {
        var tempFilePath = Path.Combine(_downloadsDirectory, $"{tool.Id}-{tool.Version}.tmp");

        try
        {
            await DownloadAsync(tool.DownloadUrl, tempFilePath, progress, ct);
            var record = InstallZip(tool, tempFilePath);
            _localStateService.Upsert(record);
            return record;
        }
        finally
        {
            try
            {
                if (File.Exists(tempFilePath)) File.Delete(tempFilePath);
            }
            catch (IOException) { }
        }
    }

    private async Task DownloadAsync(string downloadUrl, string destinationPath, IProgress<double> progress, CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("ToolPortalServer");
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
                    progress.Report((double)readTotal / totalBytes.Value);
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
            Directory.Delete(stagingDir, recursive: true);

        try
        {
            ZipFile.ExtractToDirectory(zipFilePath, stagingDir);
        }
        catch (InvalidDataException ex)
        {
            if (Directory.Exists(stagingDir))
                Directory.Delete(stagingDir, recursive: true);
            throw new PackageCorruptException("パッケージが破損しています。", ex);
        }

        // ZIP内にトップレベルのフォルダが1つだけある場合はそのフォルダを剥がす
        StripSingleTopLevelDirectory(stagingDir);

        if (Directory.Exists(finalDir))
            Directory.Delete(finalDir, recursive: true);
        Directory.Move(stagingDir, finalDir);

        return new InstalledToolRecord
        {
            Id = tool.Id,
            Version = tool.Version,
            InstallPath = finalDir,
            InstalledAt = DateTimeOffset.Now
        };
    }

    private static void StripSingleTopLevelDirectory(string dir)
    {
        var entries = Directory.GetFileSystemEntries(dir);
        if (entries.Length != 1) return;

        var singleEntry = entries[0];
        if (!Directory.Exists(singleEntry)) return;

        var temp = dir + "_strip";
        Directory.Move(singleEntry, temp);
        Directory.Delete(dir);
        Directory.Move(temp, dir);
    }
}
