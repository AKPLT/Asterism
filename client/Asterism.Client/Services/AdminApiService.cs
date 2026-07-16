using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Asterism.Client.Services.Exceptions;
using Asterism.Shared.Models;

namespace Asterism.Client.Services;

public sealed class AdminApiService : IAdminApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private string? _adminKey;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public bool IsUnlocked => _adminKey is not null;

    public AdminApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<bool> TryUnlockAsync(string adminKey, CancellationToken ct = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("AsterismServer");
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/admin/ping");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminKey);
            using var response = await client.SendAsync(request, ct);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return false;
            }

            response.EnsureSuccessStatusCode();
            _adminKey = adminKey;
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new AdminApiException("サーバーに接続できません。", ex);
        }
    }

    public void Lock() => _adminKey = null;

    public async Task<List<ToolEntry>> GetToolsAsync(CancellationToken ct = default)
    {
        using var response = await SendAsync(HttpMethod.Get, "api/admin/tools", null, ct);
        var tools = await response.Content.ReadFromJsonAsync<List<ToolEntry>>(JsonOptions, ct);
        return tools ?? new List<ToolEntry>();
    }

    public async Task<ToolEntry> CreateToolAsync(ToolEntry metadata, string packageFilePath, string? iconFilePath, CancellationToken ct = default)
    {
        using var content = BuildFormContent(metadata, packageFilePath, iconFilePath);
        using var response = await SendAsync(HttpMethod.Post, "api/admin/tools", content, ct);
        return (await response.Content.ReadFromJsonAsync<ToolEntry>(JsonOptions, ct))!;
    }

    public async Task<ToolEntry> UpdateToolAsync(string id, ToolEntry metadata, string? packageFilePath, string? iconFilePath, CancellationToken ct = default)
    {
        using var content = BuildFormContent(metadata, packageFilePath, iconFilePath);
        using var response = await SendAsync(HttpMethod.Put, $"api/admin/tools/{id}", content, ct);
        return (await response.Content.ReadFromJsonAsync<ToolEntry>(JsonOptions, ct))!;
    }

    public async Task DeleteToolAsync(string id, bool purge = false, CancellationToken ct = default)
    {
        var uri = purge ? $"api/admin/tools/{id}?purge=true" : $"api/admin/tools/{id}";
        using var response = await SendAsync(HttpMethod.Delete, uri, null, ct);
    }

    private static MultipartFormDataContent BuildFormContent(ToolEntry metadata, string? packageFilePath, string? iconFilePath)
    {
        var content = new MultipartFormDataContent
        {
            { new StringContent(JsonSerializer.Serialize(metadata, JsonOptions)), "metadata" }
        };

        if (packageFilePath is not null)
        {
            content.Add(new StreamContent(File.OpenRead(packageFilePath)), "package", Path.GetFileName(packageFilePath));
        }

        if (iconFilePath is not null)
        {
            content.Add(new StreamContent(File.OpenRead(iconFilePath)), "icon", Path.GetFileName(iconFilePath));
        }

        return content;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string requestUri, HttpContent? content, CancellationToken ct)
    {
        if (_adminKey is null)
        {
            throw new AdminApiException("管理者モードがロックされています。");
        }

        try
        {
            var client = _httpClientFactory.CreateClient("AsterismServer");
            using var request = new HttpRequestMessage(method, requestUri) { Content = content };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _adminKey);

            var response = await client.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                throw new AdminApiException($"サーバーエラー ({(int)response.StatusCode}): {body}");
            }

            return response;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new AdminApiException("サーバーとの通信に失敗しました。", ex);
        }
    }
}
