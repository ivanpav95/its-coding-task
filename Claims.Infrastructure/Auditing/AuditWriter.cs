using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Claims.Infrastructure.Auditing;

/// <summary>
/// Saves queued audit messages to SQL Server: whatever is already queued, up to the batch size, in one DI scope and one
/// <c>SaveChanges</c>. If a batch fails, its messages are saved one by one, so a bad row only loses itself.
/// </summary>
public class AuditWriter(
    Channel<AuditMessage> channel,
    IServiceScopeFactory scopeFactory,
    ILogger<AuditWriter> logger,
    int batchSize) : BackgroundService
{
    private readonly int _batchSize = batchSize > 0
        ? batchSize
        : throw new ArgumentOutOfRangeException(nameof(batchSize), batchSize, "The audit batch size must be positive.");

    // No stopping token: on shutdown StopAsync closes the channel and the loop ends once the queue is drained.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var batch = new List<AuditMessage>(_batchSize);
        while (await channel.Reader.WaitToReadAsync(CancellationToken.None))
        {
            while (batch.Count < _batchSize && channel.Reader.TryRead(out var message))
            {
                batch.Add(message);
            }

            await SaveAsync(batch);
            batch.Clear();
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

    private async Task SaveAsync(IReadOnlyList<AuditMessage> batch)
    {
        var failure = await SaveInNewScopeAsync(batch);
        if (failure is null)
        {
            return;
        }

        if (batch.Count == 1)
        {
            logger.LogError(failure, "Failed to save {Message}.", batch[0]);
            return;
        }

        logger.LogWarning(failure, "Failed to save a batch of {Count} audit messages; saving them one by one.", batch.Count);
        foreach (var message in batch)
        {
            var messageFailure = await SaveInNewScopeAsync([message]);
            if (messageFailure is not null)
            {
                logger.LogError(messageFailure, "Failed to save {Message}.", message);
            }
        }
    }

    /// <returns>The exception if saving failed; <c>null</c> if all messages were saved.</returns>
    private async Task<Exception?> SaveInNewScopeAsync(IReadOnlyList<AuditMessage> messages)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AuditContext>();
            foreach (var message in messages)
            {
                AddTo(context, message);
            }

            await context.SaveChangesAsync();
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    private static void AddTo(AuditContext context, AuditMessage message)
    {
        switch (message.Entity)
        {
            case AuditedEntity.Cover:
                context.CoverAudits.Add(new CoverAudit
                {
                    CoverId = message.EntityId, HttpRequestType = message.HttpRequestType, Created = message.Created
                });
                break;
            case AuditedEntity.Claim:
                context.ClaimAudits.Add(new ClaimAudit
                {
                    ClaimId = message.EntityId, HttpRequestType = message.HttpRequestType, Created = message.Created
                });
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(message), message.Entity, "Unknown audited entity.");
        }
    }
}
