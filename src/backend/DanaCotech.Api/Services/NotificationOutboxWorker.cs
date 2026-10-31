using System.Net.Mail;
using DanaCotech.Infrastructure.Data;
using Microsoft.Data.SqlClient;

namespace DanaCotech.Api.Services;

public sealed class NotificationOutboxWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    EmailNotificationSender emailSender,
    GoogleCalendarClient calendarClient,
    ILogger<NotificationOutboxWorker> logger) : BackgroundService
{
    private const int BatchSize = 10;
    private const int MaxAttempts = 5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Calendar is dispatched first so a newly-created Meet link can be included in the confirmation email.
                if (configuration.GetValue<bool>("GoogleCalendar:Enabled"))
                    await DispatchAsync("calendar", stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Google Calendar outbox dispatch failed");
            }

            try
            {
                if (configuration.GetValue<bool>("Email:Enabled"))
                    await DispatchAsync("email", stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Email outbox dispatch failed");
            }

            try { await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }

    private async Task DispatchAsync(string channel, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<ISqlConnectionFactory>();
        await using var connection = factory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var rows = await ClaimAsync(connection, channel, cancellationToken);
        foreach (var row in rows)
        {
            try
            {
                string? providerReference = null;
                if (channel == "email")
                {
                    await emailSender.SendAsync(row.Email, row.Name, row.Template, row.Payload, cancellationToken);
                }
                else
                {
                    var calendarEvent = await calendarClient.CreateEventAsync(row.Id, row.Email, row.Name, row.Payload, cancellationToken);
                    using var payload = new OutboxPayload(row.Payload);
                    var bookingId = payload.GetGuid("id", "bookingId");
                    var meetingUrl = calendarEvent.MeetingUrl ?? payload.GetString("meetingUrl");
                    if (bookingId is Guid id)
                        await SaveCalendarDetailsAsync(connection, row.UserId, id, calendarEvent.EventId, meetingUrl, cancellationToken);
                    providerReference = calendarEvent.EventId;
                }

                await MarkAsync(connection, row.Id, true, providerReference, null, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Outbox item {OutboxId} failed on channel {Channel}", row.Id, channel);
                await MarkAsync(connection, row.Id, false, null, DescribeException(exception), cancellationToken);
            }
        }
    }

    private static string DescribeException(Exception exception)
    {
        var details = new List<string>();
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            var detail = $"{current.GetType().Name}: {current.Message}";
            if (current is SmtpException smtpException)
                detail += $" (SMTP status: {smtpException.StatusCode})";
            details.Add(detail);
        }

        return string.Join(" -> ", details);
    }

    private static async Task<IReadOnlyCollection<OutboxItem>> ClaimAsync(SqlConnection connection, string channel, CancellationToken cancellationToken)
    {
        var items = new List<OutboxItem>();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await using (var command = new SqlCommand("""
            SET XACT_ABORT ON;

            UPDATE dbo.NotificationOutbox
            SET Status = CASE WHEN Attempts >= @MaxAttempts THEN N'failed' ELSE N'pending' END,
                ClaimedAtUtc = NULL,
                LastError = CASE WHEN Attempts >= @MaxAttempts THEN N'Delivery lease expired after the final attempt.' ELSE LastError END
            WHERE Status = N'processing'
              AND ClaimedAtUtc < DATEADD(MINUTE, -10, SYSUTCDATETIME());

            DECLARE @Claimed TABLE
            (
                Id BIGINT NOT NULL,
                UserId UNIQUEIDENTIFIER NOT NULL,
                TemplateKey NVARCHAR(120) NOT NULL,
                PayloadJson NVARCHAR(MAX) NOT NULL
            );

            ;WITH CandidateRows AS
            (
                SELECT TOP (@BatchSize) Id
                FROM dbo.NotificationOutbox WITH (UPDLOCK, READPAST, ROWLOCK)
                WHERE Channel = @Channel
                  AND Status = N'pending'
                  AND Attempts < @MaxAttempts
                ORDER BY CreatedAtUtc, Id
            )
            UPDATE outbox
            SET Status = N'processing',
                Attempts = Attempts + 1,
                ClaimedAtUtc = SYSUTCDATETIME()
            OUTPUT inserted.Id, inserted.UserId, inserted.TemplateKey, inserted.PayloadJson
                INTO @Claimed (Id, UserId, TemplateKey, PayloadJson)
            FROM dbo.NotificationOutbox AS outbox
            INNER JOIN CandidateRows AS candidate ON candidate.Id = outbox.Id;

            SELECT claimed.Id, claimed.UserId, account.Email, account.Name, claimed.TemplateKey, claimed.PayloadJson
            FROM @Claimed AS claimed
            INNER JOIN dbo.ClientAccounts AS account ON account.Id = claimed.UserId
            ORDER BY claimed.Id;
            """, connection, transaction))
        {
            command.Parameters.AddWithValue("@Channel", channel);
            command.Parameters.AddWithValue("@BatchSize", BatchSize);
            command.Parameters.AddWithValue("@MaxAttempts", MaxAttempts);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(new OutboxItem(
                    reader.GetInt64(0),
                    reader.GetGuid(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.GetString(5)));
            }
        }
        await transaction.CommitAsync(cancellationToken);
        return items;
    }

    private static async Task SaveCalendarDetailsAsync(
        SqlConnection connection,
        Guid userId,
        Guid bookingId,
        string eventId,
        string? meetingUrl,
        CancellationToken cancellationToken)
    {
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await using var command = new SqlCommand("""
            UPDATE dbo.SessionBookings
            SET ExternalCalendarEventId = @EventId,
                MeetingUrl = COALESCE(NULLIF(@MeetingUrl, N''), MeetingUrl)
            WHERE UserId = @UserId AND (Id = @BookingId OR SessionId = @BookingId);

            IF @MeetingUrl IS NOT NULL AND @MeetingUrl <> N''
            BEGIN
                DECLARE @BookingIndex INT;
                SELECT TOP (1) @BookingIndex = TRY_CONVERT(INT, booking.[key])
                FROM dbo.ClientJourneyStates AS journey
                CROSS APPLY OPENJSON(journey.StateJson, '$.bookings') AS booking
                WHERE journey.UserId = @UserId
                  AND TRY_CONVERT(UNIQUEIDENTIFIER, COALESCE(JSON_VALUE(booking.value, '$.id'), JSON_VALUE(booking.value, '$.Id'))) = @BookingId;

                IF @BookingIndex IS NOT NULL
                BEGIN
                    UPDATE dbo.ClientJourneyStates
                    SET StateJson = JSON_MODIFY(StateJson, CONCAT('$.bookings[', @BookingIndex, '].meetingUrl'), @MeetingUrl),
                        UpdatedAtUtc = SYSUTCDATETIME()
                    WHERE UserId = @UserId;
                END;

                UPDATE dbo.NotificationOutbox
                SET PayloadJson = JSON_MODIFY(PayloadJson, '$.MeetingUrl', @MeetingUrl)
                WHERE UserId = @UserId
                  AND Channel = N'email'
                  AND TemplateKey = N'booking-confirmation'
                  AND Status = N'pending'
                  AND TRY_CONVERT(UNIQUEIDENTIFIER, COALESCE(JSON_VALUE(PayloadJson, '$.Id'), JSON_VALUE(PayloadJson, '$.id'), JSON_VALUE(PayloadJson, '$.BookingId'), JSON_VALUE(PayloadJson, '$.bookingId'), JSON_VALUE(PayloadJson, '$.SessionId'), JSON_VALUE(PayloadJson, '$.sessionId'))) = @BookingId;
            END;
            """, connection, transaction);
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@BookingId", bookingId);
        command.Parameters.AddWithValue("@EventId", eventId);
        command.Parameters.AddWithValue("@MeetingUrl", (object?)meetingUrl ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task MarkAsync(
        SqlConnection connection,
        long id,
        bool sent,
        string? providerReference,
        string? error,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.NotificationOutbox
            SET Status = CASE WHEN @Sent = 1 THEN N'sent' WHEN Attempts >= @MaxAttempts THEN N'failed' ELSE N'pending' END,
                ProviderReference = COALESCE(@Reference, ProviderReference),
                ClaimedAtUtc = NULL,
                SentAtUtc = CASE WHEN @Sent = 1 THEN SYSUTCDATETIME() ELSE SentAtUtc END,
                LastError = CASE WHEN @Sent = 1 THEN NULL ELSE LEFT(@Error, 2000) END
            WHERE Id = @Id AND Status = N'processing';
            """;
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@Sent", sent);
        command.Parameters.AddWithValue("@MaxAttempts", MaxAttempts);
        command.Parameters.AddWithValue("@Reference", (object?)providerReference ?? DBNull.Value);
        command.Parameters.AddWithValue("@Error", (object?)error ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private sealed record OutboxItem(long Id, Guid UserId, string Email, string Name, string Template, string Payload);
}
