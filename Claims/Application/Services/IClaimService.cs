using Claims.Application.Contracts;

namespace Claims.Application.Services;

public interface IClaimService
{
    Task<IReadOnlyList<ClaimResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ClaimResponse?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<CreateClaimResult> CreateAsync(CreateClaimRequest request, CancellationToken cancellationToken = default);

    /// <returns><c>false</c> if no claim with this id exists.</returns>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
