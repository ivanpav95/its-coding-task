using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Claims.Infrastructure.Auditing;

/// <summary>Saves queued audit messages to SQL Server, one DI scope per message.</summary>
public class AuditWriter(
    Channel<AuditMessage> channel,
    IServiceScopeFactory scopeFactory,
    ILogger<AuditWriter> logger) : BackgroundService
{
    // No stopping token: on shutdown StopAsync closes the channel and the loop ends once the queue is drained.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in channel.Reader.ReadAllAsync(CancellationToken.None))
        {
            await SaveAsync(message);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        channel.Writer.TryComplete();
        await base.StopAsync(cancellationToken);

        if (channel.Reader.Count > 0)
        {
            logger.LogWarning("Shutdown timed out; {Count} audit messages were not saved.", channel.Reader.Count);
        }
    }

    private async Task SaveAsync(AuditMessage message)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AuditContext>();
            context.Add(ToEntity(message));
            await context.SaveChangesAsync();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to save {Message}.", message);
        }
    }

    private static object ToEntity(AuditMessage message) => message.Entity switch
    {
        AuditedEntity.Cover => new CoverAudit
        {
            CoverId = message.EntityId, HttpRequestType = message.HttpRequestType, Created = message.Created
        },
        AuditedEntity.Claim => new ClaimAudit
        {
            ClaimId = message.EntityId, HttpRequestType = message.HttpRequestType, Created = message.Created
        },
        _ => throw new ArgumentOutOfRangeException(nameof(message), message.Entity, "Unknown audited entity.")
    };
}
