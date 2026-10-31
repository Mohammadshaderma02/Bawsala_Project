using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace DanaCotech.Api.Services;

public sealed class GoogleCalendarClient(IHttpClientFactory httpClientFactory, IConfiguration configuration)
{
    private readonly SemaphoreSlim tokenLock = new(1, 1);
    private string? accessToken;
    private DateTimeOffset accessTokenExpiresAt;

    public async Task<CalendarEventResult> CreateEventAsync(long outboxId, string attendeeEmail, string attendeeName, string payload, CancellationToken cancellationToken)
    {
        using var data = new OutboxPayload(payload);
        var startsAt = data.GetDateTimeOffset("startsAtUtc", "startAtUtc")
            ?? throw new InvalidOperationException("Calendar invitation is missing its start time.");
        var endsAt = data.GetDateTimeOffset("endsAtUtc", "endAtUtc") ?? startsAt.AddHours(1);
        if (endsAt <= startsAt) throw new InvalidOperationException("Calendar invitation end time must be after its start time.");

        var calendarId = configuration["GoogleCalendar:CalendarId"] ?? "primary";
        var timeZoneId = configuration["GoogleCalendar:TimeZone"] ?? "Asia/Amman";
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var eventId = CreateEventId(outboxId);
        var language = data.GetString("language");
        var arabic = string.Equals(language, "ar", StringComparison.OrdinalIgnoreCase);
        var summary = SessionTitle(data, arabic);
        var meetingUrl = data.GetString("meetingUrl");
        var description = arabic
            ? "جلسة مهنية مع دانا غرايبة عبر منصة بوصلة."
            : "Career session with Dana Gharaibeh, booked through Bawsala.";
        if (!string.IsNullOrWhiteSpace(meetingUrl))
            description += $"\n\n{(arabic ? "رابط الجلسة" : "Meeting link")}: {meetingUrl}";

        var eventBody = new Dictionary<string, object?>
        {
            ["id"] = eventId,
            ["summary"] = summary,
            ["description"] = description,
            ["start"] = new { dateTime = FormatCalendarTime(startsAt, timeZone), timeZone = timeZoneId },
            ["end"] = new { dateTime = FormatCalendarTime(endsAt, timeZone), timeZone = timeZoneId },
            ["attendees"] = new[] { new { email = attendeeEmail, displayName = attendeeName } },
            ["extendedProperties"] = new { @private = new Dictionary<string, string> { ["bawsalaOutboxId"] = outboxId.ToString(CultureInfo.InvariantCulture) } }
        };

        if (!string.IsNullOrWhiteSpace(meetingUrl)) eventBody["location"] = meetingUrl;
        if (configuration.GetValue("GoogleCalendar:CreateMeetLink", true) && string.IsNullOrWhiteSpace(meetingUrl))
        {
            eventBody["conferenceData"] = new
            {
                createRequest = new
                {
                    requestId = eventId,
                    conferenceSolutionKey = new { type = "hangoutsMeet" }
                }
            };
        }

        var encodedCalendarId = Uri.EscapeDataString(calendarId);
        var encodedEventId = Uri.EscapeDataString(eventId);
        var apiBase = $"https://www.googleapis.com/calendar/v3/calendars/{encodedCalendarId}/events";
        using var request = await CreateAuthorizedRequestAsync(HttpMethod.Post,
            $"{apiBase}?conferenceDataVersion=1&sendUpdates=all", cancellationToken);
        request.Content = new StringContent(JsonSerializer.Serialize(eventBody), Encoding.UTF8, "application/json");

        using var response = await httpClientFactory.CreateClient("GoogleCalendar").SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            return await GetExistingEventAsync($"{apiBase}/{encodedEventId}", eventId, cancellationToken);
        }

        var result = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Google Calendar event creation failed ({(int)response.StatusCode}): {result}");

        return ParseEvent(result, eventId);
    }

    private async Task<CalendarEventResult> GetExistingEventAsync(string eventUrl, string expectedEventId, CancellationToken cancellationToken)
    {
        using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, eventUrl, cancellationToken);
        using var response = await httpClientFactory.CreateClient("GoogleCalendar").SendAsync(request, cancellationToken);
        var result = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Google Calendar could not retrieve the existing event ({(int)response.StatusCode}): {result}");
        return ParseEvent(result, expectedEventId);
    }

    private async Task<HttpRequestMessage> CreateAuthorizedRequestAsync(HttpMethod method, string url, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync(cancellationToken));
        return request;
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(accessToken) && accessTokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1)) return accessToken;

        await tokenLock.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrWhiteSpace(accessToken) && accessTokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1)) return accessToken;

            var clientId = Required("GoogleCalendar:ClientId");
            var clientSecret = Required("GoogleCalendar:ClientSecret");
            var refreshToken = Required("GoogleCalendar:RefreshToken");
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["refresh_token"] = refreshToken,
                ["grant_type"] = "refresh_token"
            });
            using var response = await httpClientFactory.CreateClient("GoogleCalendar").PostAsync(
                "https://oauth2.googleapis.com/token", content, cancellationToken);
            var result = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Google OAuth token refresh failed ({(int)response.StatusCode}): {result}");

            using var document = JsonDocument.Parse(result);
            accessToken = document.RootElement.GetProperty("access_token").GetString();
            var expiresIn = document.RootElement.TryGetProperty("expires_in", out var expiresValue) && expiresValue.TryGetInt32(out var seconds)
                ? seconds
                : 3600;
            if (string.IsNullOrWhiteSpace(accessToken)) throw new InvalidOperationException("Google OAuth returned an empty access token.");
            accessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(60, expiresIn));
            return accessToken;
        }
        finally
        {
            tokenLock.Release();
        }
    }

    private string Required(string key) =>
        string.IsNullOrWhiteSpace(configuration[key])
            ? throw new InvalidOperationException($"{key} is missing.")
            : configuration[key]!;

    private static string CreateEventId(long outboxId) => $"b{outboxId:x8}";

    private static string FormatCalendarTime(DateTimeOffset value, TimeZoneInfo zone) =>
        TimeZoneInfo.ConvertTime(value, zone).ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);

    private static string SessionTitle(OutboxPayload data, bool arabic)
    {
        var explicitTitle = arabic
            ? data.GetString("titleAr", "sessionTitleAr")
            : data.GetString("titleEn", "sessionTitleEn");
        if (!string.IsNullOrWhiteSpace(explicitTitle)) return explicitTitle;

        var sessionType = data.GetString("sessionType", "focus") ?? "coaching";
        var displayName = string.Join(" ", sessionType.Replace('-', ' ').Replace('_', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
        return arabic ? $"جلسة {displayName} مع دانا" : $"{displayName} with Dana";
    }

    private static CalendarEventResult ParseEvent(string json, string fallbackId)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var eventId = root.TryGetProperty("id", out var idValue) ? idValue.GetString() ?? fallbackId : fallbackId;
        string? meetingUrl = root.TryGetProperty("hangoutLink", out var hangoutLink) ? hangoutLink.GetString() : null;
        if (string.IsNullOrWhiteSpace(meetingUrl) && root.TryGetProperty("conferenceData", out var conferenceData) &&
            conferenceData.TryGetProperty("entryPoints", out var entryPoints) && entryPoints.ValueKind == JsonValueKind.Array)
        {
            foreach (var entryPoint in entryPoints.EnumerateArray())
            {
                if (entryPoint.TryGetProperty("entryPointType", out var type) && type.GetString() == "video" &&
                    entryPoint.TryGetProperty("uri", out var uri))
                {
                    meetingUrl = uri.GetString();
                    break;
                }
            }
        }
        return new CalendarEventResult(eventId, meetingUrl);
    }
}

public sealed record CalendarEventResult(string EventId, string? MeetingUrl);
