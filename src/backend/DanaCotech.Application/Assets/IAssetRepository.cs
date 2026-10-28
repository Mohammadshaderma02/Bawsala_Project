using DanaCotech.Domain.Assets;

namespace DanaCotech.Application.Assets;

public interface IAssetRepository
{
    Task<AssetContent?> GetByKeyAsync(string assetKey, string language, CancellationToken cancellationToken);
    Task SaveAsync(AssetContent asset, CancellationToken cancellationToken);
}
