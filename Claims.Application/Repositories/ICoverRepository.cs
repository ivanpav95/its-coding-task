using Claims.Domain;

namespace Claims.Application.Repositories;

public interface ICoverRepository
{
    Task<IReadOnlyList<Cover>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Cover?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task AddAsync(Cover cover, CancellationToken cancellationToken = default);

    /// <returns><c>false</c> if no cover with this id exists.</returns>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
