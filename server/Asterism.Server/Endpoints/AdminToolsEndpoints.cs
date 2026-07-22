using System.Drawing;
using System.Drawing.Imaging;
using System.IO.Compression;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Asterism.Server.Services;
using Asterism.Shared.Models;

namespace Asterism.Server.Endpoints;

public static class AdminToolsEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public static void MapAdminToolsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin");

        group.MapGet("/tools", async (ManifestStore store, CancellationToken ct) =>
        {
            var manifest = await store.ReadAsync(ct);
            return Results.Ok(manifest.Tools);
        });

        group.MapPost("/tools", CreateToolAsync);
        group.MapPut("/tools/{id}", UpdateToolAsync);

        group.MapDelete("/tools/{id}", async (string id, bool purge, ManifestStore store, IWebHostEnvironment env, CancellationToken ct) =>
        {
            if (!IsSafeSegment(id))
                return Results.BadRequest("idは英数字・ハイフン・アンダースコア・ドットのみ使用できます。");

            try
            {
                string? iconUrl = null;
                if (purge)
                {
                    var manifest = await store.ReadAsync(ct);
                    iconUrl = manifest.Tools.FirstOrDefault(t => t.Id == id)?.IconUrl;
                }

                await store.RemoveToolAsync(id, ct);

                if (purge)
                {
                    DeleteAllPackageVersions(env.WebRootPath, id);
                    DeleteFileIfExists(env.WebRootPath, iconUrl);
                }

                return Results.NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(ex.Message);
            }
        });
    }

    private static async Task<IResult> CreateToolAsync(
        HttpRequest request, ManifestStore store, IWebHostEnvironment env, CancellationToken ct)
    {
        if (!request.HasFormContentType)
            return Results.BadRequest("multipart/form-data で送信してください。");

        var form = await request.ReadFormAsync(ct);
        ToolEntry? entry;
        try
        {
            entry = JsonSerializer.Deserialize<ToolEntry>(form["metadata"].ToString(), JsonOptions);
        }
        catch (JsonException)
        {
            return Results.BadRequest("metadataのJSONが不正です。");
        }

        if (entry is null || string.IsNullOrWhiteSpace(entry.Id))
            return Results.BadRequest("id は必須です。");

        if (!IsSafeSegment(entry.Id) || !IsSafeSegment(entry.Version))
            return Results.BadRequest("idとversionは英数字・ハイフン・アンダースコア・ドットのみ使用できます。");

        if (string.IsNullOrWhiteSpace(entry.ExecutablePath))
            return Results.BadRequest("executablePathは必須です。");

        var package = form.Files["package"];
        if (package is null || package.Length == 0)
            return Results.BadRequest("packageファイルは必須です。");

        if (!Path.GetExtension(package.FileName).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest("パッケージファイルは .zip のみ許可されます。");

        var existingManifest = await store.ReadAsync(ct);
        if (existingManifest.Tools.Any(t => t.Id == entry.Id))
            return Results.Conflict($"id '{entry.Id}' は既に存在します。");

        var packageFileName = $"{entry.Id}-{entry.Version}.zip";
        var packagePath = Path.Combine(env.WebRootPath, "tools", packageFileName);
        await SaveFileAsync(package, Path.Combine(env.WebRootPath, "tools"), packageFileName, ct);
        entry.DownloadUrl = $"tools/{packageFileName}";
        entry.PackageType = PackageType.Zip;

        entry.IconUrl = TryExtractIconFromPackage(packagePath, entry.ExecutablePath, env, entry.Id) ?? "";

        var now = DateTimeOffset.UtcNow;
        entry.CreatedAt = now;
        entry.UpdatedAt = now;

        try
        {
            var saved = await store.AddToolAsync(entry, ct);
            return Results.Created($"/api/admin/tools/{saved.Id}", saved);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(ex.Message);
        }
    }

    private static async Task<IResult> UpdateToolAsync(
        string id, HttpRequest request, ManifestStore store, IWebHostEnvironment env, CancellationToken ct)
    {
        if (!IsSafeSegment(id))
            return Results.BadRequest("idは英数字・ハイフン・アンダースコア・ドットのみ使用できます。");

        if (!request.HasFormContentType)
            return Results.BadRequest("multipart/form-data で送信してください。");

        var form = await request.ReadFormAsync(ct);
        ToolEntry? entry;
        try
        {
            entry = JsonSerializer.Deserialize<ToolEntry>(form["metadata"].ToString(), JsonOptions);
        }
        catch (JsonException)
        {
            return Results.BadRequest("metadataのJSONが不正です。");
        }

        if (entry is null || !IsSafeSegment(entry.Version))
            return Results.BadRequest("versionは英数字・ハイフン・アンダースコア・ドットのみ使用できます。");

        if (string.IsNullOrWhiteSpace(entry.ExecutablePath))
            return Results.BadRequest("executablePathは必須です。");

        entry.Id = id;
        entry.PackageType = PackageType.Zip;

        var package = form.Files["package"];

        var manifest = await store.ReadAsync(ct);
        var current = manifest.Tools.FirstOrDefault(t => t.Id == id);
        if (current is null)
            return Results.NotFound($"id '{id}' は見つかりません。");

        if (entry.Version != current.Version && package is not { Length: > 0 })
            return Results.BadRequest("バージョンを変更する場合はpackageファイルが必須です。");

        string? autoIconUrl = null;
        if (package is { Length: > 0 })
        {
            if (!Path.GetExtension(package.FileName).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                return Results.BadRequest("パッケージファイルは .zip のみ許可されます。");

            var packageFileName = $"{id}-{entry.Version}.zip";
            var packagePath = Path.Combine(env.WebRootPath, "tools", packageFileName);
            await SaveFileAsync(package, Path.Combine(env.WebRootPath, "tools"), packageFileName, ct);
            entry.DownloadUrl = $"tools/{packageFileName}";

            autoIconUrl = TryExtractIconFromPackage(packagePath, entry.ExecutablePath, env, id);
            if (autoIconUrl is not null) entry.IconUrl = autoIconUrl;
        }

        try
        {
            var updated = await store.UpdateToolAsync(id, existing =>
            {
                if (package is not { Length: > 0 }) entry.DownloadUrl = existing.DownloadUrl;
                if (autoIconUrl is null) entry.IconUrl = existing.IconUrl;
                entry.CreatedAt = existing.CreatedAt;
                entry.UpdatedAt = DateTimeOffset.UtcNow;
                return entry;
            }, ct);
            return Results.Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(ex.Message);
        }
    }

    private static async Task SaveFileAsync(IFormFile file, string directory, string fileName, CancellationToken ct)
    {
        Directory.CreateDirectory(directory);
        await using var stream = File.Create(Path.Combine(directory, fileName));
        await file.CopyToAsync(stream, ct);
    }

    private static void DeleteAllPackageVersions(string webRoot, string id)
    {
        var toolsDir = Path.Combine(webRoot, "tools");
        if (!Directory.Exists(toolsDir)) return;

        foreach (var file in Directory.GetFiles(toolsDir, $"{id}-*.zip"))
        {
            File.Delete(file);
        }
    }

    [SupportedOSPlatform("windows")]
    private static string? TryExtractIconFromPackage(string zipPath, string executableRelativePath, IWebHostEnvironment env, string id)
    {
        if (!OperatingSystem.IsWindows() || string.IsNullOrWhiteSpace(executableRelativePath))
            return null;

        var normalizedTarget = executableRelativePath.Replace('\\', '/').TrimStart('/');
        var tempExePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.exe");

        try
        {
            using var archive = ZipFile.OpenRead(zipPath);

            // クライアントのInstallService.StripSingleTopLevelDirectoryと同じ規則で
            // 展開後にトップレベルフォルダが1つだけ剥がされるケースに合わせて照合する
            var entryNames = archive.Entries.Select(e => e.FullName.Replace('\\', '/')).ToList();
            var topLevelSegments = entryNames.Select(n => n.Split('/')[0]).Distinct().ToList();
            string? stripPrefix = null;
            if (topLevelSegments.Count == 1 && entryNames.Any(n => n.Contains('/')))
            {
                stripPrefix = topLevelSegments[0] + "/";
            }

            var entry = archive.Entries.FirstOrDefault(e =>
            {
                var name = e.FullName.Replace('\\', '/');
                if (stripPrefix is not null && name.StartsWith(stripPrefix, StringComparison.OrdinalIgnoreCase))
                    name = name[stripPrefix.Length..];
                return name.Equals(normalizedTarget, StringComparison.OrdinalIgnoreCase);
            });
            if (entry is null)
                return null;

            entry.ExtractToFile(tempExePath, overwrite: true);

            using var icon = Icon.ExtractAssociatedIcon(tempExePath);
            if (icon is null)
                return null;

            var iconsDir = Path.Combine(env.WebRootPath, "icons");
            Directory.CreateDirectory(iconsDir);
            var fileName = $"{id}.png";
            using (var bitmap = icon.ToBitmap())
            using (var stream = File.Create(Path.Combine(iconsDir, fileName)))
            {
                bitmap.Save(stream, ImageFormat.Png);
            }

            return $"icons/{fileName}";
        }
        catch
        {
            return null;
        }
        finally
        {
            if (File.Exists(tempExePath))
                File.Delete(tempExePath);
        }
    }

    private static bool IsSafeSegment(string value) =>
        !string.IsNullOrWhiteSpace(value) && Regex.IsMatch(value, "^[a-zA-Z0-9._-]+$");

    private static void DeleteFileIfExists(string webRoot, string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return;
        var fullPath = Path.GetFullPath(Path.Combine(webRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (fullPath.StartsWith(webRoot, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath))
            File.Delete(fullPath);
    }
}
