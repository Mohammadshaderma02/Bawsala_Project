using DanaCotech.Application.Assets;
using DanaCotech.Domain.Assets;

namespace DanaCotech.Infrastructure.Assets;

public sealed class InMemoryAssetRepository : IAssetRepository
{
    private readonly Dictionary<string, AssetContent> assets = new(StringComparer.OrdinalIgnoreCase);
    private const string TransparentPixel =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=";

    public Task<AssetContent?> GetByKeyAsync(string assetKey, string language, CancellationToken cancellationToken)
    {
        AssetContent? asset = assets.GetValueOrDefault(assetKey) ?? new(assetKey, $"{assetKey}.png", "image/png", TransparentPixel, assetKey);
        return Task.FromResult<AssetContent?>(asset);
    }

    public Task SaveAsync(AssetContent asset, CancellationToken cancellationToken) { assets[asset.AssetKey] = asset; return Task.CompletedTask; }
}
