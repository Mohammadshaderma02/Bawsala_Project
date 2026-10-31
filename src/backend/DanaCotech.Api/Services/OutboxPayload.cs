using System.Globalization;
using System.Text.Json;

namespace DanaCotech.Api.Services;

internal sealed class OutboxPayload : IDisposable
{
    private readonly JsonDocument document;

    public OutboxPayload(string json) => document = JsonDocument.Parse(json);

    public string? GetString(params string[] names)
    {
        string? emptyValue = null;
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (!names.Any(name => property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) continue;
            if (property.Value.ValueKind == JsonValueKind.Null) continue;
            var value = property.Value.ValueKind == JsonValueKind.String
                ? property.Value.GetString()
                : property.Value.ToString();
            if (!string.IsNullOrWhiteSpace(value)) return value;
            emptyValue ??= value;
        }
        return emptyValue;
    }

    public DateTimeOffset? GetDateTimeOffset(params string[] names)
    {
        var value = GetString(names);
        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed)
            ? parsed
            : null;
    }

    public Guid? GetGuid(params string[] names) =>
        Guid.TryParse(GetString(names), out var value) ? value : null;

    public void Dispose() => document.Dispose();
}
