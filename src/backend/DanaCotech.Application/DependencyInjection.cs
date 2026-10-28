using DanaCotech.Application.Assets;
using DanaCotech.Application.Content;
using Microsoft.Extensions.DependencyInjection;
using DanaCotech.Application.Platform;

namespace DanaCotech.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<ILandingContentService, LandingContentService>();
        services.AddScoped<IPlatformService, PlatformService>();
        return services;
    }
}
