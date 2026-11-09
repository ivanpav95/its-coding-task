using Claims.Application.Repositories;
using Claims.Domain;

namespace Claims.UnitTests.Fakes;

public class InMemoryClaimRepository : IClaimRepository
{
    private readonly Dictionary<string, Claim> _claims = [];

    public IReadOnlyCollection<Claim> Stored => _claims.Values;

    public Task<IReadOnlyList<Claim>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Claim>>(_claims.Values.ToList());

    public Task<Claim?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_claims.GetValueOrDefault(id));

    public Task AddAsync(Claim claim, CancellationToken cancellationToken = default)
    {
        _claims.Add(claim.Id, claim);
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_claims.Remove(id));

    public Task<bool> AnyForCoverAsync(string coverId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_claims.Values.Any(claim => claim.CoverId == coverId));
}
