using Claims.Application.Auditing;

namespace Claims.UnitTests.Fakes;

public record Audit(string Entity, string Id, AuditAction Action);

public class RecordingAuditer : IAuditer
{
    public List<Audit> Audits { get; } = [];

    public ValueTask AuditClaimAsync(string id, AuditAction action)
    {
        Audits.Add(new Audit("Claim", id, action));
        return ValueTask.CompletedTask;
    }

    public ValueTask AuditCoverAsync(string id, AuditAction action)
    {
        Audits.Add(new Audit("Cover", id, action));
        return ValueTask.CompletedTask;
    }
}
