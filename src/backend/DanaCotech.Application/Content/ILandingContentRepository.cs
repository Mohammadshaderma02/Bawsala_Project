using DanaCotech.Domain.Content;

namespace DanaCotech.Application.Content;

public interface ILandingContentRepository
{
    Task<LandingContent?> GetLandingContentAsync(string language, CancellationToken cancellationToken);
    Task SaveLandingContentAsync(string language, LandingContent content, CancellationToken cancellationToken);
}
