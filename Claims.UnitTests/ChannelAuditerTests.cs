using System.Threading.Channels;
using Claims.Infrastructure.Auditing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Claims.UnitTests;

public class ChannelAuditerTests
{
    private static readonly DateTimeOffset Now = new(2027, 1, 10, 12, 30, 0, TimeSpan.Zero);

    private readonly Channel<AuditMessage> _channel = Channel.CreateUnbounded<AuditMessage>();

    private ChannelAuditer CreateAuditer() =>
        new(_channel.Writer, new FakeTimeProvider(Now), NullLogger<ChannelAuditer>.Instance);

    [Fact]
    public async Task A_cover_audit_is_queued_with_the_UTC_time_of_the_request()
    {
        await CreateAuditer().AuditCoverAsync("cover-1", "POST");

        Assert.True(_channel.Reader.TryRead(out var message));
        Assert.Equal(new AuditMessage(AuditedEntity.Cover, "cover-1", "POST", Now.UtcDateTime), message);
        Assert.Equal(DateTimeKind.Utc, message.Created.Kind);
    }

    [Fact]
    public async Task A_claim_audit_is_queued()
    {
        await CreateAuditer().AuditClaimAsync("claim-1", "DELETE");

        Assert.True(_channel.Reader.TryRead(out var message));
        Assert.Equal(new AuditMessage(AuditedEntity.Claim, "claim-1", "DELETE", Now.UtcDateTime), message);
    }

    [Fact]
    public async Task Auditing_after_shutdown_closed_the_queue_does_not_fail_the_request()
    {
        _channel.Writer.Complete();

        await CreateAuditer().AuditCoverAsync("cover-1", "DELETE");

        Assert.False(_channel.Reader.TryRead(out _));
    }
}
