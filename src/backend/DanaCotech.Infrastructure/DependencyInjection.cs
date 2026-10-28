using DanaCotech.Application.Assets;
using DanaCotech.Application.Content;
using DanaCotech.Infrastructure.Assets;
using DanaCotech.Infrastructure.Content;
using DanaCotech.Infrastructure.Data;
using DanaCotech.Application.Platform;
using DanaCotech.Infrastructure.Platform;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DanaCotech.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DanaCotechDb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddSingleton<IAssetRepository, InMemoryAssetRepository>();
            services.AddSingleton<ILandingContentRepository, InMemoryLandingContentRepository>();
            services.AddSingleton<IPlatformRepository, InMemoryPlatformRepository>();
            return services;
        }

        services.AddSingleton<ISqlConnectionFactory>(_ => new SqlConnectionFactory(connectionString));
        services.AddScoped<IAssetRepository, SqlAssetRepository>();
        services.AddScoped<ILandingContentRepository, SqlLandingContentRepository>();
        services.AddScoped<IPlatformRepository, SqlPlatformRepository>();

        return services;
    }
}
