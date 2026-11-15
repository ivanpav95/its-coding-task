using System.Threading.Channels;
using Claims.Application.Auditing;
using Claims.Application.Repositories;
using Claims.Infrastructure.Auditing;
using Claims.Infrastructure.Health;
using Claims.Infrastructure.Persistence;
using Claims.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;

namespace Claims.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>Tag of the checks that decide whether the app can serve requests (<c>/health/ready</c>).</summary>
    public const string ReadinessTag = "ready";

    private static readonly TimeSpan HealthCheckTimeout = TimeSpan.FromSeconds(3);

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Read when first used, so configuration added later (e.g. by the integration tests) still applies.
        string RequiredConnectionString(string name) =>
            configuration.GetConnectionString(name)
            ?? throw new InvalidOperationException($"Connection string '{name}' is missing.");

        services.AddDbContext<AuditContext>(options =>
            options.UseSqlServer(RequiredConnectionString("Audit")));

        var mongoDatabaseName = configuration.GetRequiredSection("MongoDb:DatabaseName").Value!;

        services.AddSingleton<IMongoClient>(_ => new MongoClient(RequiredConnectionString("Claims")));

        services.AddDbContext<ClaimsContext>((provider, options) =>
            options.UseMongoDB(provider.GetRequiredService<IMongoClient>(), mongoDatabaseName));

        services.AddScoped<ICoverRepository, CoverRepository>();
        services.AddScoped<IClaimRepository, ClaimRepository>();

        var auditQueueCapacity = configuration.GetRequiredSection("Auditing:QueueCapacity").Get<int>();
        services.AddSingleton(Channel.CreateBounded<AuditMessage>(new BoundedChannelOptions(auditQueueCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true
        }));
        services.AddSingleton(provider => provider.GetRequiredService<Channel<AuditMessage>>().Writer);
        services.AddSingleton<IAuditer, ChannelAuditer>();
        var auditBatchSize = configuration.GetRequiredSection("Auditing:BatchSize").Get<int>();
        services.AddHostedService(provider => ActivatorUtilities.CreateInstance<AuditWriter>(provider, auditBatchSize));

        var businessTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
            configuration.GetRequiredSection("BusinessTimeZone").Value!);
        services.AddSingleton<TimeProvider>(new BusinessTimeProvider(businessTimeZone));

        // Audits are queued, so the API still serves without SQL Server: degraded (200), not unhealthy (503).
        services.AddHealthChecks()
            .AddCheck<AuditDatabaseHealthCheck>("audit-database", HealthStatus.Degraded, [ReadinessTag])
            .AddCheck<MongoDbHealthCheck>("claims-database", HealthStatus.Unhealthy, [ReadinessTag]);

        // Without a timeout, a dead MongoDB would hold the check for the driver's 30 s server-selection timeout.
        services.Configure<HealthCheckServiceOptions>(options =>
        {
            foreach (var registration in options.Registrations)
            {
                registration.Timeout = HealthCheckTimeout;
            }
        });

        return services;
    }

    public static void MigrateAuditDatabase(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AuditContext>().Database.Migrate();
    }
}
