using System.Text.Json;
using DanaCotech.Application.Content;
using DanaCotech.Domain.Content;
using DanaCotech.Infrastructure.Data;
using Microsoft.Data.SqlClient;

namespace DanaCotech.Infrastructure.Content;

public sealed class SqlLandingContentRepository(ISqlConnectionFactory connectionFactory) : ILandingContentRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<LandingContent?> GetLandingContentAsync(string language, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            """
            SELECT TOP (1) ContentJson
            FROM dbo.LandingPages
            WHERE LanguageCode = @LanguageCode AND IsPublished = 1
            ORDER BY UpdatedAtUtc DESC
            """,
            connection);

        command.Parameters.AddWithValue("@LanguageCode", language);

        var json = await command.ExecuteScalarAsync(cancellationToken) as string;

        return string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<LandingContent>(json, JsonOptions);
    }

    public async Task SaveLandingContentAsync(string language, LandingContent content, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        await using var unpublishCommand = new SqlCommand(
            "UPDATE dbo.LandingPages SET IsPublished = 0, UpdatedAtUtc = SYSUTCDATETIME() WHERE LanguageCode = @LanguageCode AND IsPublished = 1",
            connection,
            transaction);
        unpublishCommand.Parameters.AddWithValue("@LanguageCode", language);
        await unpublishCommand.ExecuteNonQueryAsync(cancellationToken);

        await using var publishCommand = new SqlCommand(
            "INSERT INTO dbo.LandingPages (LanguageCode, ContentJson, IsPublished) VALUES (@LanguageCode, @ContentJson, 1)",
            connection,
            transaction);
        publishCommand.Parameters.AddWithValue("@LanguageCode", language);
        publishCommand.Parameters.AddWithValue("@ContentJson", JsonSerializer.Serialize(content, JsonOptions));
        await publishCommand.ExecuteNonQueryAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
