using Claims.Domain;

namespace Claims.Application.Services;

public interface ICoverService
{
    Task<IReadOnlyList<Cover>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Cover?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<Cover> CreateAsync(Cover cover, CancellationToken cancellationToken = default);

    /// <returns><c>false</c> if no cover with this id exists.</returns>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

    decimal ComputePremium(DateTime startDate, DateTime endDate, CoverType coverType);
}
