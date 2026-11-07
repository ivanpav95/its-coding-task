using Claims.Domain;

namespace Claims.Application.Services;

public interface IClaimService
{
    Task<IReadOnlyList<Claim>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Claim?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<Claim> CreateAsync(Claim claim, CancellationToken cancellationToken = default);

    /// <returns><c>false</c> if no claim with this id exists.</returns>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
