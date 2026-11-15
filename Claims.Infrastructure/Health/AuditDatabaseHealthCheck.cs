using Claims.Infrastructure.Auditing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Claims.Infrastructure.Health;

/// <summary>Healthy when the audit database accepts a connection; a failure is reported by the health check service.</summary>
public sealed class AuditDatabaseHealthCheck(AuditContext auditContext) : IHealthCheck
{
    // SqlClient ignores cancellation while connecting, so the check's timeout can't stop it; its own connect timeout can.
    private const int ConnectTimeoutSeconds = 3;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var connectionString = new SqlConnectionStringBuilder(auditContext.Database.GetConnectionString())
        {
            ConnectTimeout = ConnectTimeoutSeconds
        }.ConnectionString;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return HealthCheckResult.Healthy();
    }
}
