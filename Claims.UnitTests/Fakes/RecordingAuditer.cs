using Claims.Application.Auditing;

namespace Claims.UnitTests.Fakes;

public record Audit(string Entity, string Id, string HttpRequestType);

public class RecordingAuditer : IAuditer
{
    public List<Audit> Audits { get; } = [];

    public ValueTask AuditClaimAsync(string id, string httpRequestType)
    {
        Audits.Add(new Audit("Claim", id, httpRequestType));
        return ValueTask.CompletedTask;
    }

    public ValueTask AuditCoverAsync(string id, string httpRequestType)
    {
        Audits.Add(new Audit("Cover", id, httpRequestType));
        return ValueTask.CompletedTask;
    }
}
