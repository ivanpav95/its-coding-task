using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Claims.Infrastructure.Health;

/// <summary>Healthy when MongoDB answers a <c>ping</c>; a failure or a timeout is reported by the health check service.</summary>
public sealed class MongoDbHealthCheck(IMongoClient client) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        await client.GetDatabase("admin").RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken);
        return HealthCheckResult.Healthy();
    }
}
