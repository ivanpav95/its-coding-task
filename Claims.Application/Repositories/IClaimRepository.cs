using Claims.Domain;

namespace Claims.Application.Repositories;

public interface IClaimRepository
{
    Task<IReadOnlyList<Claim>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Claim?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task AddAsync(Claim claim, CancellationToken cancellationToken = default);

    /// <returns><c>false</c> if no claim with this id exists.</returns>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

    Task<bool> AnyForCoverAsync(string coverId, CancellationToken cancellationToken = default);
}
