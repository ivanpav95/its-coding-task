using Claims.Domain;

namespace Claims.Application.Repositories;

public interface IClaimRepository
{
    /// <summary>
    /// Claims ordered by the moment of the damage, then id (ordinal), so the order is always the same and pages never
    /// overlap or skip one.
    /// </summary>
    Task<IReadOnlyList<Claim>> GetPageAsync(int skip, int take, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    Task<Claim?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task AddAsync(Claim claim, CancellationToken cancellationToken = default);

    /// <returns><c>false</c> if no claim with this id exists.</returns>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

    Task<bool> AnyForCoverAsync(string coverId, CancellationToken cancellationToken = default);
}
