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

    // Template formula, unchanged apart from the DateOnly parameters. Replaced in Task 5.
    public decimal ComputePremium(DateOnly startDate, DateOnly endDate, CoverType coverType)
    {
        var multiplier = 1.3m;
        if (coverType == CoverType.Yacht)
        {
            multiplier = 1.1m;
        }

        if (coverType == CoverType.PassengerShip)
        {
            multiplier = 1.2m;
        }

        if (coverType == CoverType.Tanker)
        {
            multiplier = 1.5m;
        }

        var premiumPerDay = 1250 * multiplier;
        var insuranceLength = endDate.DayNumber - startDate.DayNumber;
        var totalPremium = 0m;

        for (var i = 0; i < insuranceLength; i++)
        {
            if (i < 30) totalPremium += premiumPerDay;
            if (i < 180 && coverType == CoverType.Yacht) totalPremium += premiumPerDay - premiumPerDay * 0.05m;
            else if (i < 180) totalPremium += premiumPerDay - premiumPerDay * 0.02m;
            if (i < 365 && coverType != CoverType.Yacht) totalPremium += premiumPerDay - premiumPerDay * 0.03m;
            else if (i < 365) totalPremium += premiumPerDay - premiumPerDay * 0.08m;
        }

        return totalPremium;
    }
}
