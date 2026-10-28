using DanaCotech.Application.Common;
using DanaCotech.Domain.Assets;

namespace DanaCotech.Application.Assets;

public interface IAssetService
{
    Task<ServiceResponse<AssetContent>> GetByKeyAsync(string assetKey, string language, CancellationToken cancellationToken);
    Task<ServiceResponse<AssetContent>> SaveAsync(string assetKey, string fileName, string mimeType, string base64Content, string? altText, CancellationToken cancellationToken);
}
