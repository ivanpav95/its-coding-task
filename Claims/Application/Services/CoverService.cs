using Claims.Application.Auditing;
using Claims.Application.Repositories;
using Claims.Domain;

namespace Claims.Application.Services;

public class CoverService(ICoverRepository covers, IAuditer auditer) : ICoverService
{
    public Task<IReadOnlyList<Cover>> GetAllAsync(CancellationToken cancellationToken = default) =>
        covers.GetAllAsync(cancellationToken);

    public Task<Cover?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        covers.GetByIdAsync(id, cancellationToken);

    public async Task<Cover> CreateAsync(Cover cover, CancellationToken cancellationToken = default)
    {
        cover.Id = Guid.NewGuid().ToString();
        cover.Premium = ComputePremium(cover.StartDate, cover.EndDate, cover.Type);
        await covers.AddAsync(cover, cancellationToken);
        auditer.AuditCover(cover.Id, "POST");
        return cover;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        auditer.AuditCover(id, "DELETE");
        return await covers.DeleteAsync(id, cancellationToken);
    }

    public decimal ComputePremium(DateTime startDate, DateTime endDate, CoverType coverType)
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
        var insuranceLength = (endDate - startDate).TotalDays;
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
