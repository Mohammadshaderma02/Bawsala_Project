namespace DanaCotech.Domain.Assets;

public sealed record AssetContent(
    string AssetKey,
    string? FileName,
    string MimeType,
    string Base64Content,
    string? AltText)
{
    public string DataUrl => $"data:{MimeType};base64,{Base64Content}";
}
