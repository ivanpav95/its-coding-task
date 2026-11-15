using Claims.Domain;

namespace Claims.Application.Repositories;

public interface ICoverRepository
{
    /// <summary>
    /// Covers ordered by start date, then id (ordinal), so the order is always the same and pages never overlap or skip one.
    /// </summary>
    Task<IReadOnlyList<Cover>> GetPageAsync(int skip, int take, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    Task<Cover?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task AddAsync(Cover cover, CancellationToken cancellationToken = default);

    /// <returns><c>false</c> if no cover with this id exists.</returns>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
