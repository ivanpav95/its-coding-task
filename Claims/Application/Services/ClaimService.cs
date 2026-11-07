using Claims.Application.Auditing;
using Claims.Application.Repositories;
using Claims.Domain;

namespace Claims.Application.Services;

public class ClaimService(IClaimRepository claims, IAuditer auditer) : IClaimService
{
    public Task<IReadOnlyList<Claim>> GetAllAsync(CancellationToken cancellationToken = default) =>
        claims.GetAllAsync(cancellationToken);

    public Task<Claim?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        claims.GetByIdAsync(id, cancellationToken);

    public async Task<Claim> CreateAsync(Claim claim, CancellationToken cancellationToken = default)
    {
        claim.Id = Guid.NewGuid().ToString();
        await claims.AddAsync(claim, cancellationToken);
        auditer.AuditClaim(claim.Id, "POST");
        return claim;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        auditer.AuditClaim(id, "DELETE");
        return await claims.DeleteAsync(id, cancellationToken);
    }
}
