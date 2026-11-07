namespace Claims.Application.Auditing;

public interface IAuditer
{
    ValueTask AuditClaimAsync(string id, string httpRequestType);

    ValueTask AuditCoverAsync(string id, string httpRequestType);
}
