using Claims.Application.Auditing;
using Claims.Application.Contracts;
using Claims.Application.Repositories;
using Claims.Domain;

namespace Claims.Application.Services;

public class ClaimService(IClaimRepository claims, IAuditer auditer) : IClaimService
{
    public async Task<IReadOnlyList<ClaimResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var all = await claims.GetAllAsync(cancellationToken);
        return all.Select(ClaimResponse.From).ToList();
    }

    public async Task<ClaimResponse?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var claim = await claims.GetByIdAsync(id, cancellationToken);
        return claim is null ? null : ClaimResponse.From(claim);
    }

    public async Task<ClaimResponse> CreateAsync(CreateClaimRequest request, CancellationToken cancellationToken = default)
    {
        var claim = new Claim
        {
            Id = Guid.NewGuid().ToString(),
            CoverId = request.CoverId,
            Created = request.Created,
            Name = request.Name,
            Type = request.Type,
            DamageCost = request.DamageCost
        };

        await claims.AddAsync(claim, cancellationToken);
        auditer.AuditClaim(claim.Id, "POST");
        return ClaimResponse.From(claim);
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        auditer.AuditClaim(id, "DELETE");
        return await claims.DeleteAsync(id, cancellationToken);
    }
}
