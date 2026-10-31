using System.Text.Json;
using System.Data;
using DanaCotech.Application.Platform;
using DanaCotech.Domain.Platform;
using DanaCotech.Infrastructure.Data;
using Microsoft.Data.SqlClient;

namespace DanaCotech.Infrastructure.Platform;

public sealed class SqlPlatformRepository(ISqlConnectionFactory connectionFactory) : IPlatformRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<ClientAccount?> GetUserByEmailAsync(string email, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection(); await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand("SELECT TOP 1 Id, Name, Email, PasswordHash, PasswordSalt, Role, CreatedAtUtc, IsEmailVerified, IsDeleted, DeletedAtUtc FROM dbo.ClientAccounts WHERE Email = @Email", connection);
        command.Parameters.AddWithValue("@Email", email);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadAccount(reader) : null;
    }

    public async Task<ClientAccount?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection(); await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand("SELECT TOP 1 Id, Name, Email, PasswordHash, PasswordSalt, Role, CreatedAtUtc, IsEmailVerified, IsDeleted, DeletedAtUtc FROM dbo.ClientAccounts WHERE Id = @Id", connection);
        command.Parameters.AddWithValue("@Id", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadAccount(reader) : null;
    }

    public async Task AddUserAsync(ClientAccount account, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection(); await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand("INSERT INTO dbo.ClientAccounts (Id, Name, Email, PasswordHash, PasswordSalt, Role, CreatedAtUtc, IsEmailVerified) VALUES (@Id,@Name,@Email,@Hash,@Salt,@Role,@Created,@Verified)", connection);
        command.Parameters.AddWithValue("@Id", account.Id); command.Parameters.AddWithValue("@Name", account.Name); command.Parameters.AddWithValue("@Email", account.Email);
        command.Parameters.AddWithValue("@Hash", account.PasswordHash); command.Parameters.AddWithValue("@Salt", account.PasswordSalt); command.Parameters.AddWithValue("@Role", account.Role); command.Parameters.AddWithValue("@Created", account.CreatedAtUtc);
        command.Parameters.AddWithValue("@Verified", account.IsEmailVerified);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ClientAccount>> GetUsersAsync(CancellationToken cancellationToken)
    {
        var users = new List<ClientAccount>();
        await using var connection = connectionFactory.CreateConnection(); await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand("SELECT Id, Name, Email, PasswordHash, PasswordSalt, Role, CreatedAtUtc, IsEmailVerified, IsDeleted, DeletedAtUtc FROM dbo.ClientAccounts WHERE IsDeleted = 0 ORDER BY CreatedAtUtc DESC", connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) users.Add(ReadAccount(reader));
        return users;
    }

    public async Task<IReadOnlyCollection<AdminUserDeletionLog>> GetUserDeletionLogsAsync(CancellationToken cancellationToken)
    {
        var logs = new List<AdminUserDeletionLog>();
        await using var connection = connectionFactory.CreateConnection(); await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand("SELECT ActorUserId, ActorName, ActorEmail, TargetUserId, TargetName, TargetEmail, TargetRole, DeletedAtUtc FROM dbo.AdminUserDeletionLogs ORDER BY DeletedAtUtc DESC", connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            logs.Add(new AdminUserDeletionLog(reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetGuid(3), reader.GetString(4), reader.GetString(5), reader.GetString(6), reader.GetDateTime(7)));
        return logs;
    }

    public async Task<UserDeletionOutcome> DeleteUserAsync(Guid actorUserId, Guid userId, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection(); await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await using var command = new SqlCommand("""
            SET XACT_ABORT ON;
            DECLARE @Outcome NVARCHAR(40) = N'not-found';
            DECLARE @ActorName NVARCHAR(160), @ActorEmail NVARCHAR(320);
            DECLARE @TargetName NVARCHAR(160), @TargetEmail NVARCHAR(320), @TargetRole NVARCHAR(20);
            DECLARE @DeletedAtUtc DATETIME2;

            SELECT @ActorName = Name, @ActorEmail = Email
            FROM dbo.ClientAccounts WITH (UPDLOCK, HOLDLOCK)
            WHERE Id = @ActorUserId AND Role = N'Admin' AND IsDeleted = 0;

            IF @ActorName IS NULL
                SET @Outcome = N'actor-unavailable';
            ELSE IF @ActorUserId = @UserId
                SET @Outcome = N'self-delete';
            ELSE
            BEGIN
                SELECT @TargetName = Name, @TargetEmail = Email, @TargetRole = Role
                FROM dbo.ClientAccounts WITH (UPDLOCK, HOLDLOCK)
                WHERE Id = @UserId AND IsDeleted = 0;

                IF @TargetName IS NULL
                    SET @Outcome = N'not-found';
                ELSE IF @TargetRole = N'Admin' AND
                    (SELECT COUNT(*) FROM dbo.ClientAccounts WITH (UPDLOCK, HOLDLOCK) WHERE Role = N'Admin' AND IsDeleted = 0) <= 1
                    SET @Outcome = N'last-admin';
                ELSE
                BEGIN
                    SET @DeletedAtUtc = SYSUTCDATETIME();
                    UPDATE dbo.ClientAccounts
                    SET Name = N'Deleted user',
                        Email = N'deleted+' + CONVERT(NVARCHAR(36), @UserId) + N'@deleted.invalid',
                        PasswordHash = N'',
                        PasswordSalt = N'',
                        IsEmailVerified = 0,
                        IsDeleted = 1,
                        DeletedAtUtc = @DeletedAtUtc
                    WHERE Id = @UserId AND IsDeleted = 0;
                    IF @@ROWCOUNT = 1
                    BEGIN
                        UPDATE dbo.NotificationOutbox
                        SET Status = N'failed',
                            ClaimedAtUtc = NULL,
                            LastError = N'Recipient account was deleted before notification delivery.'
                        WHERE UserId = @UserId AND Status IN (N'pending', N'processing');

                        INSERT dbo.AdminUserDeletionLogs (ActorUserId, ActorName, ActorEmail, TargetUserId, TargetName, TargetEmail, TargetRole, DeletedAtUtc)
                        VALUES (@ActorUserId, @ActorName, @ActorEmail, @UserId, @TargetName, @TargetEmail, @TargetRole, @DeletedAtUtc);
                        SET @Outcome = N'deleted';
                    END
                END
            END;

            SELECT @Outcome, @ActorName, @ActorEmail, @TargetName, @TargetEmail, @TargetRole, @DeletedAtUtc;
            """, connection, transaction);
        command.Parameters.AddWithValue("@ActorUserId", actorUserId);
        command.Parameters.AddWithValue("@UserId", userId);
        UserDeletionOutcome outcome;
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            await reader.ReadAsync(cancellationToken);
            var result = reader.GetString(0);
            if (result != "deleted")
                outcome = new UserDeletionOutcome(result, null);
            else
            {
                var auditLog = new AdminUserDeletionLog(actorUserId, reader.GetString(1), reader.GetString(2), userId, reader.GetString(3), reader.GetString(4), reader.GetString(5), reader.GetDateTime(6));
                outcome = new UserDeletionOutcome(null, auditLog);
            }
        }
        await transaction.CommitAsync(cancellationToken);
        return outcome;
    }

    public async Task<AdminNotificationPage> GetNotificationsAsync(string? channel, string? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        var items = new List<AdminNotificationRecord>();
        await using var connection = connectionFactory.CreateConnection(); await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand("""
            SELECT COUNT_BIG(1)
            FROM dbo.NotificationOutbox AS notification
            WHERE (@Channel IS NULL OR notification.Channel = @Channel)
              AND (@Status IS NULL OR notification.Status = @Status);

            SELECT notification.Id, notification.UserId,
                   COALESCE(account.Name, N'Unknown user'),
                   CASE WHEN notification.TemplateKey = N'test-email'
                        THEN COALESCE(JSON_VALUE(notification.PayloadJson, '$.recipientEmail'), account.Email, N'')
                        ELSE COALESCE(account.Email, N'') END,
                   notification.Channel, notification.TemplateKey, notification.Status,
                   notification.ProviderReference, notification.Attempts, notification.CreatedAtUtc,
                   notification.ClaimedAtUtc, notification.SentAtUtc, notification.LastError, notification.PayloadJson
            FROM dbo.NotificationOutbox AS notification
            LEFT JOIN dbo.ClientAccounts AS account ON account.Id = notification.UserId
            WHERE (@Channel IS NULL OR notification.Channel = @Channel)
              AND (@Status IS NULL OR notification.Status = @Status)
            ORDER BY notification.CreatedAtUtc DESC, notification.Id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """, connection);
        command.Parameters.Add("@Channel", System.Data.SqlDbType.NVarChar, 30).Value = (object?)channel ?? DBNull.Value;
        command.Parameters.Add("@Status", System.Data.SqlDbType.NVarChar, 20).Value = (object?)status ?? DBNull.Value;
        command.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
        command.Parameters.AddWithValue("@PageSize", pageSize);

        long totalCount;
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            await reader.ReadAsync(cancellationToken);
            totalCount = reader.GetInt64(0);
            if (await reader.NextResultAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    items.Add(new AdminNotificationRecord(
                        reader.GetInt64(0), reader.GetGuid(1), reader.GetString(2), reader.GetString(3),
                        reader.GetString(4), reader.GetString(5), reader.GetString(6),
                        reader.IsDBNull(7) ? null : reader.GetString(7), reader.GetInt32(8), reader.GetDateTime(9),
                        reader.IsDBNull(10) ? null : reader.GetDateTime(10), reader.IsDBNull(11) ? null : reader.GetDateTime(11),
                        reader.IsDBNull(12) ? null : reader.GetString(12), reader.GetString(13)));
                }
            }
        }

        return new AdminNotificationPage(items, totalCount, page, pageSize);
    }

    public async Task<long?> ResendNotificationAsync(long notificationId, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand("""
            INSERT INTO dbo.NotificationOutbox (UserId, Channel, TemplateKey, PayloadJson, Status, Attempts, CreatedAtUtc)
            OUTPUT inserted.Id
            SELECT notification.UserId, notification.Channel, notification.TemplateKey, notification.PayloadJson,
                   N'pending', 0, SYSUTCDATETIME()
            FROM dbo.NotificationOutbox AS notification
            INNER JOIN dbo.ClientAccounts AS account ON account.Id = notification.UserId AND account.IsDeleted = 0
            WHERE notification.Id = @NotificationId
              AND notification.Channel = N'email'
              AND notification.Status = N'failed'
              AND
              (
                  notification.TemplateKey IN
                  (
                      N'welcome-enrollment', N'payment-refunded', N'career-profile-submitted',
                      N'career-profile-ready', N'booking-confirmation', N'paid-session-unavailable', N'test-email'
                  )
                  OR
                  (
                      notification.TemplateKey = N'email-verification'
                      AND account.IsEmailVerified = 0
                      AND TRY_CONVERT(DATETIME2, JSON_VALUE(notification.PayloadJson, '$.expiresAtUtc')) > SYSUTCDATETIME()
                  )
              );
            """, connection);
        command.Parameters.Add("@NotificationId", SqlDbType.BigInt).Value = notificationId;
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null || result is DBNull ? null : Convert.ToInt64(result);
    }

    public async Task<IReadOnlyCollection<(ClientAccount Account, ClientJourneyState Journey)>> GetUsersWithJourneysAsync(CancellationToken cancellationToken)
    {
        var result = new List<(ClientAccount, ClientJourneyState)>();
        await using var connection = connectionFactory.CreateConnection(); await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand("""
            SELECT a.Id, a.Name, a.Email, a.PasswordHash, a.PasswordSalt, a.Role, a.CreatedAtUtc, a.IsEmailVerified, a.IsDeleted, a.DeletedAtUtc, j.StateJson
            FROM dbo.ClientAccounts a
            LEFT JOIN dbo.ClientJourneyStates j ON j.UserId = a.Id
            WHERE a.Role = N'Client' AND a.IsDeleted = 0
            ORDER BY a.CreatedAtUtc DESC
            """, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var account = ReadAccount(reader);
            var json = reader.IsDBNull(10) ? null : reader.GetString(10);
            var journey = string.IsNullOrWhiteSpace(json) ? new ClientJourneyState { UserId = account.Id } : JsonSerializer.Deserialize<ClientJourneyState>(json, JsonOptions) ?? new ClientJourneyState { UserId = account.Id };
            result.Add((account, journey));
        }
        return result;
    }

    public async Task UpdateUserRoleAsync(Guid userId, string role, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection(); await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand("UPDATE dbo.ClientAccounts SET Role = @Role WHERE Id = @Id", connection);
        command.Parameters.AddWithValue("@Id", userId);
        command.Parameters.AddWithValue("@Role", role);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task CreatePasswordResetAsync(PasswordResetToken token, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection(); await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await using var command = new SqlCommand("""
            UPDATE dbo.PasswordResetTokens
            SET UsedAtUtc = @CreatedAtUtc
            WHERE UserId = @UserId AND UsedAtUtc IS NULL;

            INSERT INTO dbo.PasswordResetTokens (Id, UserId, TokenHash, ExpiresAtUtc, CreatedAtUtc)
            VALUES (@Id, @UserId, @TokenHash, @ExpiresAtUtc, @CreatedAtUtc);
            """, connection, transaction);
        command.Parameters.AddWithValue("@Id", token.Id);
        command.Parameters.AddWithValue("@UserId", token.UserId);
        command.Parameters.AddWithValue("@TokenHash", token.TokenHash);
        command.Parameters.AddWithValue("@ExpiresAtUtc", token.ExpiresAtUtc);
        command.Parameters.AddWithValue("@CreatedAtUtc", token.CreatedAtUtc);
        await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<bool> ResetPasswordAsync(string tokenHash, string passwordHash, string passwordSalt, DateTime usedAtUtc, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection(); await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await using var command = new SqlCommand("""
            DECLARE @UserId UNIQUEIDENTIFIER;
            SELECT TOP 1 @UserId = UserId
            FROM dbo.PasswordResetTokens AS reset WITH (UPDLOCK, HOLDLOCK)
            WHERE reset.TokenHash = @TokenHash AND reset.UsedAtUtc IS NULL AND reset.ExpiresAtUtc > @UsedAtUtc
              AND EXISTS (SELECT 1 FROM dbo.ClientAccounts AS account WHERE account.Id = reset.UserId AND account.IsDeleted = 0);

            IF @UserId IS NOT NULL
            BEGIN
                UPDATE dbo.ClientAccounts SET PasswordHash = @PasswordHash, PasswordSalt = @PasswordSalt WHERE Id = @UserId;
                UPDATE dbo.PasswordResetTokens SET UsedAtUtc = @UsedAtUtc WHERE TokenHash = @TokenHash;
                SELECT CAST(1 AS BIT);
            END
            ELSE SELECT CAST(0 AS BIT);
            """, connection, transaction);
        command.Parameters.AddWithValue("@TokenHash", tokenHash);
        command.Parameters.AddWithValue("@PasswordHash", passwordHash);
        command.Parameters.AddWithValue("@PasswordSalt", passwordSalt);
        command.Parameters.AddWithValue("@UsedAtUtc", usedAtUtc);
        var result = (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task CreateEmailVerificationAsync(EmailVerificationToken token, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection(); await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand("INSERT dbo.EmailVerificationTokens (Id,UserId,TokenHash,ExpiresAtUtc,CreatedAtUtc) VALUES (@Id,@UserId,@Hash,@Expires,@Created)", connection);
        command.Parameters.AddWithValue("@Id", token.Id); command.Parameters.AddWithValue("@UserId", token.UserId); command.Parameters.AddWithValue("@Hash", token.TokenHash); command.Parameters.AddWithValue("@Expires", token.ExpiresAtUtc); command.Parameters.AddWithValue("@Created", token.CreatedAtUtc);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> VerifyEmailAsync(string tokenHash, DateTime usedAtUtc, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection(); await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await using var command = new SqlCommand("""
            DECLARE @UserId UNIQUEIDENTIFIER;
            SELECT TOP 1 @UserId=verification.UserId FROM dbo.EmailVerificationTokens AS verification WITH (UPDLOCK,HOLDLOCK) WHERE verification.TokenHash=@Hash AND verification.UsedAtUtc IS NULL AND verification.ExpiresAtUtc>@Now
              AND EXISTS (SELECT 1 FROM dbo.ClientAccounts AS account WHERE account.Id = verification.UserId AND account.IsDeleted = 0);
            IF @UserId IS NULL SELECT CAST(0 AS BIT);
            ELSE BEGIN UPDATE dbo.ClientAccounts SET IsEmailVerified=1 WHERE Id=@UserId; UPDATE dbo.EmailVerificationTokens SET UsedAtUtc=@Now WHERE TokenHash=@Hash; SELECT CAST(1 AS BIT); END
            """, connection, transaction);
        command.Parameters.AddWithValue("@Hash", tokenHash); command.Parameters.AddWithValue("@Now", usedAtUtc);
        var verified = (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false); await transaction.CommitAsync(cancellationToken); return verified;
    }

    public async Task<ClientJourneyState> GetJourneyAsync(Guid userId, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection(); await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand("SELECT StateJson FROM dbo.ClientJourneyStates WHERE UserId = @UserId", connection);
        command.Parameters.AddWithValue("@UserId", userId);
        var json = await command.ExecuteScalarAsync(cancellationToken) as string;
        return string.IsNullOrWhiteSpace(json) ? new ClientJourneyState { UserId = userId } : JsonSerializer.Deserialize<ClientJourneyState>(json, JsonOptions) ?? new ClientJourneyState { UserId = userId };
    }

    public async Task SaveJourneyAsync(ClientJourneyState state, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection(); await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand("""
            MERGE dbo.ClientJourneyStates AS target
            USING (SELECT @UserId AS UserId) AS source ON target.UserId = source.UserId
            WHEN MATCHED THEN UPDATE SET StateJson = @StateJson, UpdatedAtUtc = SYSUTCDATETIME()
            WHEN NOT MATCHED THEN INSERT (UserId, StateJson, UpdatedAtUtc) VALUES (@UserId, @StateJson, SYSUTCDATETIME());
            """, connection);
        command.Parameters.AddWithValue("@UserId", state.UserId); command.Parameters.AddWithValue("@StateJson", JsonSerializer.Serialize(state, JsonOptions));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<PaymentTransaction> CreatePaymentAsync(PaymentTransaction payment, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection(); await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand("""
            INSERT dbo.PaymentTransactions (Id,UserId,CustomerName,CustomerEmail,ServiceType,ProductSlug,ProductTitle,Amount,Currency,Status,Provider,ProviderReference,CreatedAtUtc,PaidAtUtc,RefundedAtUtc,RefundReason)
            VALUES (@Id,@UserId,@CustomerName,@CustomerEmail,@ServiceType,@ProductSlug,@ProductTitle,@Amount,@Currency,@Status,@Provider,@ProviderReference,@CreatedAtUtc,@PaidAtUtc,@RefundedAtUtc,@RefundReason)
            """, connection);
        AddPaymentParameters(command, payment);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return payment;
    }

    public async Task<IReadOnlyCollection<PaymentTransaction>> GetPaymentsAsync(CancellationToken cancellationToken)
    {
        var payments = new List<PaymentTransaction>();
        await using var connection = connectionFactory.CreateConnection(); await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand("SELECT Id,UserId,CustomerName,CustomerEmail,ServiceType,ProductSlug,ProductTitle,Amount,Currency,Status,Provider,ProviderReference,CreatedAtUtc,PaidAtUtc,RefundedAtUtc,RefundReason FROM dbo.PaymentTransactions ORDER BY CreatedAtUtc DESC", connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) payments.Add(ReadPayment(reader));
        return payments;
    }

    public async Task<PaymentTransaction?> RefundPaymentAsync(Guid paymentId, string reason, DateTime refundedAtUtc, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection(); await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand("""
            UPDATE dbo.PaymentTransactions SET Status=N'refunded', RefundedAtUtc=@RefundedAtUtc, RefundReason=@Reason
            OUTPUT inserted.Id,inserted.UserId,inserted.CustomerName,inserted.CustomerEmail,inserted.ServiceType,inserted.ProductSlug,inserted.ProductTitle,inserted.Amount,inserted.Currency,inserted.Status,inserted.Provider,inserted.ProviderReference,inserted.CreatedAtUtc,inserted.PaidAtUtc,inserted.RefundedAtUtc,inserted.RefundReason
            WHERE Id=@Id AND Status=N'paid'
            """, connection);
        command.Parameters.AddWithValue("@Id", paymentId); command.Parameters.AddWithValue("@RefundedAtUtc", refundedAtUtc); command.Parameters.AddWithValue("@Reason", reason);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadPayment(reader) : null;
    }

    public async Task<PaymentTransaction?> SetPaymentProviderReferenceAsync(Guid paymentId, string providerReference, CancellationToken cancellationToken) => await UpdatePaymentAsync(paymentId, "ProviderReference=@Reference", providerReference, null, cancellationToken);
    public async Task<PaymentTransaction?> CompletePaymentAsync(Guid paymentId, string providerReference, DateTime paidAtUtc, CancellationToken cancellationToken) => await UpdatePaymentAsync(paymentId, "ProviderReference=@Reference,Status=N'paid',PaidAtUtc=@PaidAt", providerReference, paidAtUtc, cancellationToken);

    private async Task<PaymentTransaction?> UpdatePaymentAsync(Guid paymentId, string setClause, string reference, DateTime? paidAt, CancellationToken cancellationToken)
    {
        await using var connection=connectionFactory.CreateConnection(); await connection.OpenAsync(cancellationToken);
        await using var command=new SqlCommand($"UPDATE dbo.PaymentTransactions SET {setClause} OUTPUT inserted.Id,inserted.UserId,inserted.CustomerName,inserted.CustomerEmail,inserted.ServiceType,inserted.ProductSlug,inserted.ProductTitle,inserted.Amount,inserted.Currency,inserted.Status,inserted.Provider,inserted.ProviderReference,inserted.CreatedAtUtc,inserted.PaidAtUtc,inserted.RefundedAtUtc,inserted.RefundReason WHERE Id=@Id",connection);
        command.Parameters.AddWithValue("@Id",paymentId); command.Parameters.AddWithValue("@Reference",reference); if(paidAt.HasValue) command.Parameters.AddWithValue("@PaidAt",paidAt.Value);
        await using var reader=await command.ExecuteReaderAsync(cancellationToken); return await reader.ReadAsync(cancellationToken)?ReadPayment(reader):null;
    }

    public async Task QueueNotificationAsync(Guid userId, string channel, string templateKey, string payloadJson, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection(); await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand("INSERT INTO dbo.NotificationOutbox (UserId, Channel, TemplateKey, PayloadJson, Status, Attempts, CreatedAtUtc) VALUES (@UserId, @Channel, @TemplateKey, @PayloadJson, N'pending', 0, SYSUTCDATETIME())", connection);
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@Channel", channel);
        command.Parameters.AddWithValue("@TemplateKey", templateKey);
        command.Parameters.AddWithValue("@PayloadJson", payloadJson);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<PlatformSession>> GetSessionsAsync(bool futureOnly, CancellationToken cancellationToken)
    {
        var sessions = new Dictionary<Guid, (PlatformSession Session, List<SessionAttendee> Attendees)>();
        await using var connection = connectionFactory.CreateConnection(); await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand("""
            SELECT s.Id, s.SessionType, s.TitleEn, s.TitleAr, s.StartsAtUtc, s.EndsAtUtc, s.Capacity,
                   ISNULL(s.MeetingUrl, N''), s.IsBookable, b.UserId, a.Name, a.Email, b.CreatedAtUtc
            FROM dbo.CohortSessions s
            LEFT JOIN dbo.SessionBookings b ON b.SessionId = s.Id AND b.Status = N'confirmed'
            LEFT JOIN dbo.ClientAccounts a ON a.Id = b.UserId
            WHERE (@FutureOnly = 0 OR s.StartsAtUtc > SYSUTCDATETIME())
            ORDER BY s.StartsAtUtc, b.CreatedAtUtc
            """, connection);
        command.Parameters.AddWithValue("@FutureOnly", futureOnly);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetGuid(0);
            if (!sessions.TryGetValue(id, out var entry))
            {
                var attendees = new List<SessionAttendee>();
                entry = (new PlatformSession(id, reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetDateTime(4), reader.GetDateTime(5), reader.IsDBNull(6) ? null : reader.GetInt32(6), reader.GetString(7), reader.GetBoolean(8), attendees), attendees);
                sessions[id] = entry;
            }
            if (!reader.IsDBNull(9)) entry.Attendees.Add(new SessionAttendee(reader.GetGuid(9), reader.GetString(10), reader.GetString(11), reader.GetDateTime(12)));
        }
        return sessions.Values.Select(entry => entry.Session).ToArray();
    }

    public async Task<PlatformSession> CreateSessionAsync(PlatformSession session, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection(); await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand("""
            INSERT dbo.CohortSessions (Id, CohortId, SessionType, TitleEn, TitleAr, StartsAtUtc, EndsAtUtc, Capacity, MeetingUrl, IsBookable)
            VALUES (@Id, NULL, @Type, @TitleEn, @TitleAr, @Starts, @Ends, @Capacity, @MeetingUrl, 1)
            """, connection);
        command.Parameters.AddWithValue("@Id", session.Id); command.Parameters.AddWithValue("@Type", session.SessionType);
        command.Parameters.AddWithValue("@TitleEn", session.TitleEn); command.Parameters.AddWithValue("@TitleAr", session.TitleAr);
        command.Parameters.AddWithValue("@Starts", session.StartsAtUtc); command.Parameters.AddWithValue("@Ends", session.EndsAtUtc);
        command.Parameters.AddWithValue("@Capacity", (object?)session.Capacity ?? DBNull.Value); command.Parameters.AddWithValue("@MeetingUrl", session.MeetingUrl);
        await command.ExecuteNonQueryAsync(cancellationToken); return session;
    }

    public async Task<PlatformSession?> BookSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection(); await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await using var command = new SqlCommand("""
            IF EXISTS (SELECT 1 FROM dbo.SessionBookings WHERE UserId=@UserId AND SessionId=@SessionId AND Status=N'confirmed')
                SELECT CAST(0 AS BIT);
            ELSE IF EXISTS (SELECT 1 FROM dbo.CohortSessions s WITH (UPDLOCK, HOLDLOCK) WHERE s.Id=@SessionId AND s.IsBookable=1 AND s.StartsAtUtc>SYSUTCDATETIME() AND (s.Capacity IS NULL OR (SELECT COUNT(*) FROM dbo.SessionBookings b WHERE b.SessionId=s.Id AND b.Status=N'confirmed') < s.Capacity))
            BEGIN
                INSERT dbo.SessionBookings (Id,UserId,SessionId,Focus,StartsAtUtc,EndsAtUtc,Status,MeetingUrl)
                SELECT NEWID(),@UserId,s.Id,s.SessionType,s.StartsAtUtc,s.EndsAtUtc,N'confirmed',s.MeetingUrl FROM dbo.CohortSessions s WHERE s.Id=@SessionId;
                SELECT CAST(1 AS BIT);
            END
            ELSE SELECT CAST(0 AS BIT);
            """, connection, transaction);
        command.Parameters.AddWithValue("@UserId", userId); command.Parameters.AddWithValue("@SessionId", sessionId);
        var booked = (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
        await transaction.CommitAsync(cancellationToken);
        if (!booked) return null;
        return (await GetSessionsAsync(false, cancellationToken)).First(item => item.Id == sessionId);
    }

    private static void AddPaymentParameters(SqlCommand command, PaymentTransaction payment)
    {
        command.Parameters.AddWithValue("@Id", payment.Id); command.Parameters.AddWithValue("@UserId", (object?)payment.UserId ?? DBNull.Value);
        command.Parameters.AddWithValue("@CustomerName", payment.CustomerName); command.Parameters.AddWithValue("@CustomerEmail", payment.CustomerEmail);
        command.Parameters.AddWithValue("@ServiceType", payment.ServiceType); command.Parameters.AddWithValue("@ProductSlug", payment.ProductSlug); command.Parameters.AddWithValue("@ProductTitle", payment.ProductTitle);
        command.Parameters.AddWithValue("@Amount", payment.Amount); command.Parameters.AddWithValue("@Currency", payment.Currency); command.Parameters.AddWithValue("@Status", payment.Status);
        command.Parameters.AddWithValue("@Provider", payment.Provider); command.Parameters.AddWithValue("@ProviderReference", payment.ProviderReference); command.Parameters.AddWithValue("@CreatedAtUtc", payment.CreatedAtUtc);
        command.Parameters.AddWithValue("@PaidAtUtc", (object?)payment.PaidAtUtc ?? DBNull.Value); command.Parameters.AddWithValue("@RefundedAtUtc", (object?)payment.RefundedAtUtc ?? DBNull.Value); command.Parameters.AddWithValue("@RefundReason", payment.RefundReason);
    }

    private static PaymentTransaction ReadPayment(SqlDataReader reader) => new(reader.GetGuid(0), reader.IsDBNull(1) ? null : reader.GetGuid(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.GetString(5), reader.GetString(6), reader.GetDecimal(7), reader.GetString(8), reader.GetString(9), reader.GetString(10), reader.GetString(11), reader.GetDateTime(12), reader.IsDBNull(13) ? null : reader.GetDateTime(13), reader.IsDBNull(14) ? null : reader.GetDateTime(14), reader.GetString(15));
    private static ClientAccount ReadAccount(SqlDataReader reader) => new(reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.GetString(5), reader.GetDateTime(6), reader.GetBoolean(7), reader.GetBoolean(8), reader.IsDBNull(9) ? null : reader.GetDateTime(9));
}
