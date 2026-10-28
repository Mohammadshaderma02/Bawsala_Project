namespace DanaCotech.Domain.Common;

public sealed record LocalizedText(string En, string Ar)
{
    public string Value(string language) =>
        language.Equals("ar", StringComparison.OrdinalIgnoreCase) ? Ar : En;
}
