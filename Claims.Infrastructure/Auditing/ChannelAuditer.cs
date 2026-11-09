using System.Threading.Channels;
using Claims.Application.Auditing;
using Microsoft.Extensions.Logging;

namespace Claims.Infrastructure.Auditing;

/// <summary>Queues audit messages; <see cref="AuditWriter"/> saves them in the background.</summary>
public class ChannelAuditer(
    ChannelWriter<AuditMessage> writer,
    TimeProvider timeProvider,
    ILogger<ChannelAuditer> logger) : IAuditer
{
    public ValueTask AuditClaimAsync(string id, AuditAction action) =>
        EnqueueAsync(AuditedEntity.Claim, id, action);

    public ValueTask AuditCoverAsync(string id, AuditAction action) =>
        EnqueueAsync(AuditedEntity.Cover, id, action);

    private async ValueTask EnqueueAsync(AuditedEntity entity, string id, AuditAction action)
    {
        var message = new AuditMessage(entity, id, ToHttpRequestType(action), timeProvider.GetUtcNow().UtcDateTime);
        try
        {
            await writer.WriteAsync(message);
        }
        catch (ChannelClosedException)
        {
            logger.LogWarning("Audit queue is closed (shutting down); dropped {Message}.", message);
        }
    }

    // The audit tables keep the template's HttpRequestType column; the services only know the action.
    private static string ToHttpRequestType(AuditAction action) => action switch
    {
        AuditAction.Created => "POST",
        AuditAction.Deleted => "DELETE",
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Unknown audit action.")
    };
}
