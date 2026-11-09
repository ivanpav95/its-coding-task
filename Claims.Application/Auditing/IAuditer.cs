namespace Claims.Application.Auditing;

public interface IAuditer
{
    ValueTask AuditClaimAsync(string id, AuditAction action);

    ValueTask AuditCoverAsync(string id, AuditAction action);
}
