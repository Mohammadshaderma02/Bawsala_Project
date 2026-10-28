using System.Security.Cryptography;
using System.Text;

namespace DanaCotech.Api.Security;

public sealed class SessionTokenService(IConfiguration configuration)
{
    private readonly byte[] key = Encoding.UTF8.GetBytes(configuration["Authentication:SigningKey"] ?? "dana-cotech-development-key-change-before-production");

    public string Create(Guid userId, string role)
    {
        var payload = $"{userId:N}|{role}|{DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeSeconds()}";
        var encodedPayload = Base64Url(Encoding.UTF8.GetBytes(payload));
        var signature = Base64Url(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(encodedPayload)));
        return $"{encodedPayload}.{signature}";
    }

    public bool TryValidate(string? authorization, out Guid userId, out string role)
    {
        userId = Guid.Empty;
        role = string.Empty;
        var token = authorization?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true ? authorization[7..] : null;
        var parts = token?.Split('.');
        if (parts is not { Length: 2 }) return false;
        var expected = HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(parts[0]));
        byte[] actual;
        try { actual = FromBase64Url(parts[1]); } catch { return false; }
        if (!CryptographicOperations.FixedTimeEquals(expected, actual)) return false;
        string[] payload;
        try { payload = Encoding.UTF8.GetString(FromBase64Url(parts[0])).Split('|'); } catch { return false; }
        if (payload.Length != 3 || !Guid.TryParseExact(payload[0], "N", out userId) ||
            string.IsNullOrWhiteSpace(payload[1]) || !long.TryParse(payload[2], out var expiry) ||
            expiry <= DateTimeOffset.UtcNow.ToUnixTimeSeconds()) return false;
        role = payload[1];
        return true;
    }

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static byte[] FromBase64Url(string value)
    {
        value = value.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(value.PadRight(value.Length + (4 - value.Length % 4) % 4, '='));
    }
}
