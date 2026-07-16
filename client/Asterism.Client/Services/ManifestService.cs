using System.IO;
using System.Net.Http;
using System.Text.Json;
using Asterism.Client.Services.Exceptions;
using Asterism.Shared.Models;

namespace Asterism.Client.Services;

public sealed class ManifestService : IManifestService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _cacheFilePath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public ManifestService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;

        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Asterism");
        Directory.CreateDirectory(root);
        _cacheFilePath = Path.Combine(root, "manifest.cache.json");
    }

    public async Task<ToolManifest> GetManifestAsync(CancellationToken ct = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("AsterismServer");
            var json = await client.GetStringAsync("manifest.json", ct);

            var manifest = JsonSerializer.Deserialize<ToolManifest>(json, JsonOptions)
                ?? throw new ManifestUnavailableException("manifest.jsonの内容を解釈できませんでした。");

            try
            {
                File.WriteAllText(_cacheFilePath, json);
            }
            catch (IOException)
            {
                // キャッシュ保存の失敗は致命的ではないため無視する
            }

            return manifest;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            throw new ManifestUnavailableException("サーバーからmanifest.jsonを取得できませんでした。", ex);
        }
    }

    public ToolManifest? LoadCachedManifest()
    {
        try
        {
            if (!File.Exists(_cacheFilePath))
            {
                return null;
            }

            var json = File.ReadAllText(_cacheFilePath);
            return JsonSerializer.Deserialize<ToolManifest>(json, JsonOptions);
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            return null;
        }
    }
}
