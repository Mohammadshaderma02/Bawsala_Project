using DanaCotech.Application.Assets;
using DanaCotech.Domain.Assets;
using DanaCotech.Infrastructure.Data;
using Microsoft.Data.SqlClient;

namespace DanaCotech.Infrastructure.Assets;

public sealed class SqlAssetRepository(ISqlConnectionFactory connectionFactory) : IAssetRepository
{
    public async Task SaveAsync(AssetContent asset, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand("""
            MERGE dbo.Assets AS target
            USING (SELECT @AssetKey AS AssetKey) AS source ON target.AssetKey = source.AssetKey
            WHEN MATCHED THEN UPDATE SET FileName=@FileName, MimeType=@MimeType, Base64Content=@Base64Content, AltEn=@AltText, AltAr=@AltText, IsActive=1
            WHEN NOT MATCHED THEN INSERT (AssetKey, FileName, MimeType, Base64Content, AltEn, AltAr) VALUES (@AssetKey, @FileName, @MimeType, @Base64Content, @AltText, @AltText);
            """, connection);
        command.Parameters.AddWithValue("@AssetKey", asset.AssetKey);
        command.Parameters.AddWithValue("@FileName", (object?)asset.FileName ?? DBNull.Value);
        command.Parameters.AddWithValue("@MimeType", asset.MimeType);
        command.Parameters.AddWithValue("@Base64Content", asset.Base64Content);
        command.Parameters.AddWithValue("@AltText", (object?)asset.AltText ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<AssetContent?> GetByKeyAsync(string assetKey, string language, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            """
            SELECT TOP (1)
                AssetKey,
                FileName,
                MimeType,
                Base64Content,
                CASE WHEN @LanguageCode = N'ar' THEN AltAr ELSE AltEn END AS AltText
            FROM dbo.Assets
            WHERE AssetKey = @AssetKey AND IsActive = 1
            """,
            connection);

        command.Parameters.AddWithValue("@AssetKey", assetKey);
        command.Parameters.AddWithValue("@LanguageCode", language);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new AssetContent(
            reader.GetString(reader.GetOrdinal("AssetKey")),
            reader.IsDBNull(reader.GetOrdinal("FileName")) ? null : reader.GetString(reader.GetOrdinal("FileName")),
            reader.GetString(reader.GetOrdinal("MimeType")),
            reader.GetString(reader.GetOrdinal("Base64Content")),
            reader.IsDBNull(reader.GetOrdinal("AltText")) ? null : reader.GetString(reader.GetOrdinal("AltText")));
    }
}
