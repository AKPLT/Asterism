using System.Text.Json;
using System.Text.Json.Serialization;
using Asterism.Shared.Models;

namespace Asterism.Server.Services;

public sealed class ManifestStore
{
    private readonly string _manifestPath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public ManifestStore(IWebHostEnvironment env)
    {
        _manifestPath = Path.Combine(env.WebRootPath, "manifest.json");
    }

    public async Task<ToolManifest> ReadAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            return await ReadUnlockedAsync(ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<ToolEntry> AddToolAsync(ToolEntry entry, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var manifest = await ReadUnlockedAsync(ct);
            if (manifest.Tools.Any(t => t.Id == entry.Id))
            {
                throw new InvalidOperationException($"id '{entry.Id}' は既に存在します。");
            }

            manifest.Tools.Add(entry);
            await WriteUnlockedAsync(manifest, ct);
            return entry;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<ToolEntry> UpdateToolAsync(string id, Func<ToolEntry, ToolEntry> mutate, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var manifest = await ReadUnlockedAsync(ct);
            var index = manifest.Tools.FindIndex(t => t.Id == id);
            if (index < 0)
            {
                throw new KeyNotFoundException($"id '{id}' は見つかりません。");
            }

            var updated = mutate(manifest.Tools[index]);
            manifest.Tools[index] = updated;
            await WriteUnlockedAsync(manifest, ct);
            return updated;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task RemoveToolAsync(string id, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var manifest = await ReadUnlockedAsync(ct);
            var removed = manifest.Tools.RemoveAll(t => t.Id == id);
            if (removed == 0)
            {
                throw new KeyNotFoundException($"id '{id}' は見つかりません。");
            }

            await WriteUnlockedAsync(manifest, ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<ToolManifest> ReadUnlockedAsync(CancellationToken ct)
    {
        if (!File.Exists(_manifestPath))
        {
            return new ToolManifest();
        }

        await using var stream = File.OpenRead(_manifestPath);
        return await JsonSerializer.DeserializeAsync<ToolManifest>(stream, JsonOptions, ct) ?? new ToolManifest();
    }

    private async Task WriteUnlockedAsync(ToolManifest manifest, CancellationToken ct)
    {
        var tempPath = $"{_manifestPath}.tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, manifest, JsonOptions, ct);
        }

        File.Move(tempPath, _manifestPath, overwrite: true);
    }
}
