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

        group.MapGet("/ping", () => Results.NoContent());

        group.MapGet("/tools", async (ManifestStore store, CancellationToken ct) =>
        {
            var manifest = await store.ReadAsync(ct);
            return Results.Ok(manifest.Tools);
        });

        group.MapPost("/tools", CreateToolAsync);
        group.MapPut("/tools/{id}", UpdateToolAsync);

        group.MapDelete("/tools/{id}", async (string id, ManifestStore store, CancellationToken ct) =>
        {
            try
            {
                await store.RemoveToolAsync(id, ct);
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

        var package = form.Files["package"];
        if (package is null || package.Length == 0)
            return Results.BadRequest("packageファイルは必須です。");

        if (!Path.GetExtension(package.FileName).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest("パッケージファイルは .zip のみ許可されます。");

        var packageFileName = $"{entry.Id}-{entry.Version}.zip";
        await SaveFileAsync(package, Path.Combine(env.WebRootPath, "tools"), packageFileName, ct);
        entry.DownloadUrl = $"tools/{packageFileName}";
        entry.PackageType = PackageType.Zip;

        var icon = form.Files["icon"];
        entry.IconUrl = icon is { Length: > 0 } ? await SaveIconAsync(icon, env, entry.Id, ct) : "";

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

        entry.Id = id;
        entry.PackageType = PackageType.Zip;

        var package = form.Files["package"];
        if (package is { Length: > 0 })
        {
            if (!Path.GetExtension(package.FileName).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                return Results.BadRequest("パッケージファイルは .zip のみ許可されます。");

            var packageFileName = $"{id}-{entry.Version}.zip";
            await SaveFileAsync(package, Path.Combine(env.WebRootPath, "tools"), packageFileName, ct);
            entry.DownloadUrl = $"tools/{packageFileName}";
        }

        var icon = form.Files["icon"];
        if (icon is { Length: > 0 })
            entry.IconUrl = await SaveIconAsync(icon, env, id, ct);

        try
        {
            var updated = await store.UpdateToolAsync(id, existing =>
            {
                if (package is not { Length: > 0 }) entry.DownloadUrl = existing.DownloadUrl;
                if (icon is not { Length: > 0 }) entry.IconUrl = existing.IconUrl;
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

    private static async Task<string> SaveIconAsync(IFormFile icon, IWebHostEnvironment env, string id, CancellationToken ct)
    {
        var ext = Path.GetExtension(icon.FileName);
        var fileName = $"{id}{ext}";
        await SaveFileAsync(icon, Path.Combine(env.WebRootPath, "icons"), fileName, ct);
        return $"icons/{fileName}";
    }

    private static bool IsSafeSegment(string value) =>
        !string.IsNullOrWhiteSpace(value) && Regex.IsMatch(value, "^[a-zA-Z0-9._-]+$");
}
