using System.Threading.Channels;
using Claims.Application.Auditing;

namespace Claims.Infrastructure.Auditing;

/// <summary>Queues audit messages; <see cref="AuditWriter"/> saves them in the background.</summary>
public class ChannelAuditer(
    ChannelWriter<AuditMessage> writer,
    TimeProvider timeProvider,
    ILogger<ChannelAuditer> logger) : IAuditer
{
    public ValueTask AuditClaimAsync(string id, string httpRequestType) =>
        EnqueueAsync(AuditedEntity.Claim, id, httpRequestType);

    public ValueTask AuditCoverAsync(string id, string httpRequestType) =>
        EnqueueAsync(AuditedEntity.Cover, id, httpRequestType);

    private async ValueTask EnqueueAsync(AuditedEntity entity, string id, string httpRequestType)
    {
        var message = new AuditMessage(entity, id, httpRequestType, timeProvider.GetUtcNow().UtcDateTime);
        try
        {
            await writer.WriteAsync(message);
        }
        catch (ChannelClosedException)
        {
            logger.LogWarning("Audit queue is closed (shutting down); dropped {Message}.", message);
        }
    }
}
