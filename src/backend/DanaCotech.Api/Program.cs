using DanaCotech.Application;
using DanaCotech.Infrastructure;
using DanaCotech.Api.Security;
using System.Threading.RateLimiting;
using DanaCotech.Api.Services;
using DanaCotech.Infrastructure.Data;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

if (args.Contains("--migrate-database", StringComparer.OrdinalIgnoreCase))
{
    var connectionString = builder.Configuration.GetConnectionString("DanaCotechDb");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("ConnectionStrings:DanaCotechDb is required to run database migrations.");
    }

    var migrationsPath = Path.Combine(builder.Environment.ContentRootPath, "DatabaseMigrations");
    await DatabaseMigrationRunner.RunAsync(connectionString, migrationsPath);
    return;
}

const string frontendPolicy = "Frontend";

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApplication();
builder.Services.AddSingleton<SessionTokenService>();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<NotificationOutboxWorker>();
builder.Services.AddHttpClient(nameof(MepsGatewayClient));
builder.Services.AddScoped<MepsGatewayClient>();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("password-reset", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(15),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});
builder.Services.AddCors(options =>
{
    options.AddPolicy(frontendPolicy, policy =>
    {
        policy
            .WithOrigins("http://localhost:5174", "https://localhost:5174", "https://localhost:5175", "http://localhost:5175")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(frontendPolicy);
app.UseRateLimiter();

app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "bawsala-api", utc = DateTime.UtcNow }));
app.MapControllers();

app.Run();
