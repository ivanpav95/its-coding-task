using Claims.Application.Auditing;
using Claims.Application.Contracts;
using Claims.Application.Repositories;
using Claims.Domain;

namespace Claims.Application.Services;

public class CoverService(ICoverRepository covers, IClaimRepository claims, IAuditer auditer) : ICoverService
{
    public async Task<IReadOnlyList<CoverResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var all = await covers.GetAllAsync(cancellationToken);
        return all.Select(CoverResponse.From).ToList();
    }

    public async Task<CoverResponse?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var cover = await covers.GetByIdAsync(id, cancellationToken);
        return cover is null ? null : CoverResponse.From(cover);
    }

    public async Task<CoverResponse> CreateAsync(CreateCoverRequest request, CancellationToken cancellationToken = default)
    {
        var cover = new Cover
        {
            Id = Guid.NewGuid().ToString(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Type = request.Type,
            Premium = ComputePremium(request.StartDate, request.EndDate, request.Type)
        };

        await covers.AddAsync(cover, cancellationToken);
        auditer.AuditCover(cover.Id, "POST");
        return CoverResponse.From(cover);
    }

    public async Task<DeleteCoverResult> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (await claims.AnyForCoverAsync(id, cancellationToken))
        {
            return DeleteCoverResult.HasClaims;
        }

        if (!await covers.DeleteAsync(id, cancellationToken))
        {
            return DeleteCoverResult.NotFound;
        }

        auditer.AuditCover(id, "DELETE");
        return DeleteCoverResult.Deleted;
    }

    public decimal ComputePremium(DateOnly startDate, DateOnly endDate, CoverType coverType) =>
        PremiumCalculator.Calculate(startDate, endDate, coverType);
}
