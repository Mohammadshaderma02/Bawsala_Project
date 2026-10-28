using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace DanaCotech.Infrastructure.Data;

public static class DatabaseMigrationRunner
{
    public static async Task RunAsync(string connectionString, string migrationsPath, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(migrationsPath))
        {
            throw new DirectoryNotFoundException($"Database migrations directory was not found: {migrationsPath}");
        }

        var migrationFiles = Directory.GetFiles(migrationsPath, "*.sql")
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        const string createHistorySql = """
            IF OBJECT_ID(N'dbo.SchemaMigrations', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.SchemaMigrations
                (
                    MigrationName NVARCHAR(260) NOT NULL CONSTRAINT PK_SchemaMigrations PRIMARY KEY,
                    ScriptHash CHAR(64) NOT NULL,
                    AppliedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_SchemaMigrations_AppliedAtUtc DEFAULT SYSUTCDATETIME()
                );
            END;
            """;
        await using (var createHistory = new SqlCommand(createHistorySql, connection))
        {
            await createHistory.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var migrationFile in migrationFiles)
        {
            var migrationName = Path.GetFileName(migrationFile);
            var script = await File.ReadAllTextAsync(migrationFile, cancellationToken);
            var scriptHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(script))).ToLowerInvariant();

            const string historySql = "SELECT ScriptHash FROM dbo.SchemaMigrations WHERE MigrationName = @name;";
            await using var historyCommand = new SqlCommand(historySql, connection);
            historyCommand.Parameters.AddWithValue("@name", migrationName);
            var existingHash = (string?)await historyCommand.ExecuteScalarAsync(cancellationToken);

            if (existingHash is not null)
            {
                if (!string.Equals(existingHash, scriptHash, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Migration '{migrationName}' was changed after it was applied. Add a new numbered migration instead.");
                }

                Console.WriteLine($"Skipping applied migration {migrationName}");
                continue;
            }

            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                var batches = Regex.Split(script, @"^\s*GO\s*(?:--.*)?$", RegexOptions.Multiline | RegexOptions.IgnoreCase)
                    .Where(batch => !string.IsNullOrWhiteSpace(batch));

                foreach (var batch in batches)
                {
                    await using var migrationCommand = new SqlCommand(batch, connection, transaction)
                    {
                        CommandTimeout = 180
                    };
                    await migrationCommand.ExecuteNonQueryAsync(cancellationToken);
                }

                const string insertHistorySql = """
                    INSERT INTO dbo.SchemaMigrations (MigrationName, ScriptHash, AppliedAtUtc)
                    VALUES (@name, @hash, SYSUTCDATETIME());
                    """;
                await using var insertHistory = new SqlCommand(insertHistorySql, connection, transaction);
                insertHistory.Parameters.AddWithValue("@name", migrationName);
                insertHistory.Parameters.AddWithValue("@hash", scriptHash);
                await insertHistory.ExecuteNonQueryAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                Console.WriteLine($"Applied migration {migrationName}");
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
