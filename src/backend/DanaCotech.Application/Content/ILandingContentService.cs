using DanaCotech.Application.Common;
using DanaCotech.Domain.Content;

namespace DanaCotech.Application.Content;

public interface ILandingContentService
{
    Task<ServiceResponse<LandingContent>> GetLandingContentAsync(string language, CancellationToken cancellationToken);
    Task<ServiceResponse<LandingContent>> SaveLandingContentAsync(string language, LandingContent content, CancellationToken cancellationToken);
}
