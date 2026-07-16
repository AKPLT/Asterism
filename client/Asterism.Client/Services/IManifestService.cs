using Asterism.Client.Models;

namespace Asterism.Client.Services;

public interface IManifestService
{
    Task<ToolManifest> GetManifestAsync(CancellationToken ct = default);

    /// <summary>直近取得成功時のキャッシュを読み込む。キャッシュが無い/壊れている場合はnull。</summary>
    ToolManifest? LoadCachedManifest();
}
