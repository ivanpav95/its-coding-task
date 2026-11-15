using Claims.Application.Contracts;

namespace Claims.Application.Services;

public interface ICoverService
{
    Task<GetPageResult<CoverResponse>> GetPageAsync(PageRequest request, CancellationToken cancellationToken = default);

    Task<CoverResponse?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<CreateCoverResult> CreateAsync(CreateCoverRequest request, CancellationToken cancellationToken = default);

    Task<DeleteCoverResult> DeleteAsync(string id, CancellationToken cancellationToken = default);

    Task<ComputePremiumResult> ComputePremiumAsync(CreateCoverRequest request, CancellationToken cancellationToken = default);
}
