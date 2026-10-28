using DanaCotech.Application.Common;
using DanaCotech.Domain.Assets;

namespace DanaCotech.Application.Assets;

public sealed class AssetService(IAssetRepository repository) : IAssetService
{
    private static readonly string[] AllowedTypes = [
        "image/", "video/", "application/pdf",
        "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
    ];

    public async Task<ServiceResponse<AssetContent>> SaveAsync(string assetKey, string fileName, string mimeType, string base64Content, string? altText, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(assetKey) || !AllowedTypes.Any(prefix => mimeType.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            return ServiceResponse<AssetContent>.Fail("Only images, videos, PDF, Word, and Excel files are supported");
        var asset = new AssetContent(assetKey.Trim(), fileName, mimeType, base64Content, altText);
        await repository.SaveAsync(asset, cancellationToken);
        return ServiceResponse<AssetContent>.Ok(asset, "Asset uploaded successfully");
    }

    public async Task<ServiceResponse<AssetContent>> GetByKeyAsync(string assetKey, string language, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(assetKey))
        {
            return ServiceResponse<AssetContent>.Fail("Asset key is required");
        }

        var normalizedLanguage = language.Equals("ar", StringComparison.OrdinalIgnoreCase) ? "ar" : "en";
        var asset = await repository.GetByKeyAsync(assetKey.Trim(), normalizedLanguage, cancellationToken);

        return asset is null
            ? ServiceResponse<AssetContent>.Fail("Asset was not found")
            : ServiceResponse<AssetContent>.Ok(asset);
    }
}
