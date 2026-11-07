using Claims.Application.Contracts;
using Claims.Domain;

namespace Claims.Application.Services;

public interface ICoverService
{
    Task<IReadOnlyList<CoverResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<CoverResponse?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<CoverResponse> CreateAsync(CreateCoverRequest request, CancellationToken cancellationToken = default);

    /// <returns><c>false</c> if no cover with this id exists.</returns>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

    decimal ComputePremium(DateOnly startDate, DateOnly endDate, CoverType coverType);
}
