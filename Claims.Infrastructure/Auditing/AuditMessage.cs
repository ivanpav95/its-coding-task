namespace Claims.Infrastructure.Auditing;

public enum AuditedEntity
{
    Cover,
    Claim
}

public record AuditMessage(AuditedEntity Entity, string EntityId, string HttpRequestType, DateTime Created);
