using System.Net;
using System.Net.Mail;
using System.Text.Json;
using DanaCotech.Infrastructure.Data;
using Microsoft.Data.SqlClient;

namespace DanaCotech.Api.Services;

public sealed class NotificationOutboxWorker(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<NotificationOutboxWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { if (configuration.GetValue<bool>("Email:Enabled")) await DispatchAsync(stoppingToken); if (configuration.GetValue<bool>("GoogleCalendar:Enabled")) await DispatchCalendarAsync(stoppingToken); }
            catch (Exception exception) { logger.LogError(exception, "Notification outbox dispatch failed"); }
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task DispatchAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope(); var factory = scope.ServiceProvider.GetRequiredService<ISqlConnectionFactory>();
        await using var connection = factory.CreateConnection(); await connection.OpenAsync(cancellationToken);
        var rows = new List<(long Id, string Email, string Name, string Template, string Payload)>();
        await using (var command = new SqlCommand("""
            SELECT TOP (10) n.Id,a.Email,a.Name,n.TemplateKey,n.PayloadJson
            FROM dbo.NotificationOutbox n JOIN dbo.ClientAccounts a ON a.Id=n.UserId
            WHERE n.Status=N'pending' AND n.Channel=N'email' AND n.Attempts < 5 ORDER BY n.CreatedAtUtc
            """, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken)) while (await reader.ReadAsync(cancellationToken)) rows.Add((reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4)));
        foreach (var row in rows)
        {
            try { await SendAsync(row.Email, row.Name, row.Template, row.Payload, cancellationToken); await MarkAsync(connection, row.Id, true, null, cancellationToken); }
            catch (Exception exception) { await MarkAsync(connection, row.Id, false, exception.Message, cancellationToken); }
        }
    }

    private async Task SendAsync(string email, string name, string template, string payload, CancellationToken cancellationToken)
    {
        var host = configuration["Email:SmtpHost"] ?? throw new InvalidOperationException("Email:SmtpHost is missing"); var port = configuration.GetValue("Email:SmtpPort", 587);
        using var client = new SmtpClient(host, port) { EnableSsl = configuration.GetValue("Email:EnableSsl", true), Credentials = new NetworkCredential(configuration["Email:Username"], configuration["Email:Password"]) };
        var from = configuration["Email:FromAddress"] ?? configuration["Email:Username"] ?? throw new InvalidOperationException("Email:FromAddress is missing");
        using var message = new MailMessage(from, email) { Subject = Subject(template), IsBodyHtml = true, Body = Body(name, template, payload) };
        await client.SendMailAsync(message, cancellationToken);
    }

    private async Task DispatchCalendarAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope(); var factory = scope.ServiceProvider.GetRequiredService<ISqlConnectionFactory>();
        await using var connection = factory.CreateConnection(); await connection.OpenAsync(cancellationToken);
        var rows = new List<(long Id, string Email, string Payload)>();
        await using (var command = new SqlCommand("SELECT TOP (10) n.Id,a.Email,n.PayloadJson FROM dbo.NotificationOutbox n JOIN dbo.ClientAccounts a ON a.Id=n.UserId WHERE n.Status=N'pending' AND n.Channel=N'calendar' AND n.Attempts<5 ORDER BY n.CreatedAtUtc", connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken)) while (await reader.ReadAsync(cancellationToken)) rows.Add((reader.GetInt64(0), reader.GetString(1), reader.GetString(2)));
        foreach (var row in rows)
        {
            try
            {
                using var payload = JsonDocument.Parse(row.Payload); var root = payload.RootElement; var start = root.GetProperty("startsAtUtc").GetDateTime(); var end = root.TryGetProperty("endAtUtc", out var endValue) || root.TryGetProperty("endsAtUtc", out endValue) ? endValue.GetDateTime() : start.AddHours(1);
                var body = JsonSerializer.Serialize(new { summary = "Career session with Dana", description = "Dana Gharaibeh Career Platform session", start = new { dateTime = start.ToString("O"), timeZone = "Asia/Amman" }, end = new { dateTime = end.ToString("O"), timeZone = "Asia/Amman" }, attendees = new[] { new { email = row.Email } }, conferenceData = new { createRequest = new { requestId = Guid.NewGuid().ToString("N"), conferenceSolutionKey = new { type = "hangoutsMeet" } } } });
                var calendarId = Uri.EscapeDataString(configuration["GoogleCalendar:CalendarId"] ?? "primary"); using var request = new HttpRequestMessage(HttpMethod.Post, $"https://www.googleapis.com/calendar/v3/calendars/{calendarId}/events?conferenceDataVersion=1&sendUpdates=all"); request.Headers.Authorization = new("Bearer", configuration["GoogleCalendar:AccessToken"]); request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
                using var response = await new HttpClient().SendAsync(request, cancellationToken); var result = await response.Content.ReadAsStringAsync(cancellationToken); if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Google Calendar returned {(int)response.StatusCode}: {result}");
                using var resultJson = JsonDocument.Parse(result); var reference = resultJson.RootElement.TryGetProperty("id", out var id) ? id.GetString() : null; await MarkCalendarAsync(connection, row.Id, true, reference, null, cancellationToken);
            }
            catch (Exception exception) { await MarkCalendarAsync(connection, row.Id, false, null, exception.Message, cancellationToken); }
        }
    }

    private static string Subject(string key) => key switch { "email-verification" => "Verify your Dana Career Platform account", "password-reset" => "Reset your password", "career-profile-ready" => "Your Career Clarity Profile is ready", "booking-confirmation" => "Your session is confirmed", "payment-refunded" => "Your refund is confirmed", _ => "Dana Career Platform update" };
    private static string Body(string name, string template, string payload)
    {
        using var document = JsonDocument.Parse(payload); string? Link(params string[] keys) { foreach (var key in keys) if (document.RootElement.TryGetProperty(key, out var value)) return value.ToString(); return null; }
        var link = Link("verificationUrl", "resetUrl", "meetingUrl"); var action = string.IsNullOrWhiteSpace(link) ? "" : $"<p><a style='background:#20295b;color:white;padding:12px 20px;text-decoration:none;border-radius:8px' href='{WebUtility.HtmlEncode(link)}'>Open secure link</a></p>";
        return $"<div style='font-family:Arial;max-width:620px;margin:auto;padding:32px'><h2 style='color:#20295b'>Dana Gharaibeh Career Platform</h2><p>Hello {WebUtility.HtmlEncode(name)},</p><p>{WebUtility.HtmlEncode(Subject(template))}.</p>{action}<p style='color:#667085'>If you did not request this message, you can safely ignore it.</p></div>";
    }

    private static async Task MarkAsync(SqlConnection connection, long id, bool sent, string? error, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("UPDATE dbo.NotificationOutbox SET Status=@Status,Attempts=Attempts+1,SentAtUtc=CASE WHEN @Sent=1 THEN SYSUTCDATETIME() ELSE SentAtUtc END,LastError=@Error WHERE Id=@Id", connection);
        command.Parameters.AddWithValue("@Id", id); command.Parameters.AddWithValue("@Status", sent ? "sent" : "pending"); command.Parameters.AddWithValue("@Sent", sent); command.Parameters.AddWithValue("@Error", (object?)error ?? DBNull.Value); await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task MarkCalendarAsync(SqlConnection connection, long id, bool sent, string? reference, string? error, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("UPDATE dbo.NotificationOutbox SET Status=@Status,Attempts=Attempts+1,ProviderReference=@Reference,SentAtUtc=CASE WHEN @Sent=1 THEN SYSUTCDATETIME() ELSE SentAtUtc END,LastError=@Error WHERE Id=@Id", connection);
        command.Parameters.AddWithValue("@Id", id); command.Parameters.AddWithValue("@Status", sent ? "sent" : "pending"); command.Parameters.AddWithValue("@Sent", sent); command.Parameters.AddWithValue("@Reference", (object?)reference ?? DBNull.Value); command.Parameters.AddWithValue("@Error", (object?)error ?? DBNull.Value); await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
