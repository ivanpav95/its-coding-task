using Claims.Application.Repositories;
using Claims.Domain;

namespace Claims.UnitTests.Fakes;

public class InMemoryCoverRepository : ICoverRepository
{
    private readonly Dictionary<string, Cover> _covers = [];

    public IReadOnlyCollection<Cover> Stored => _covers.Values;

    public Task<IReadOnlyList<Cover>> GetPageAsync(int skip, int take, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Cover>>(_covers.Values
            .OrderBy(cover => cover.StartDate)
            .ThenBy(cover => cover.Id, StringComparer.Ordinal)
            .Skip(skip)
            .Take(take)
            .ToList());

    public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(_covers.Count);

    public Task<Cover?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_covers.GetValueOrDefault(id));

    public Task AddAsync(Cover cover, CancellationToken cancellationToken = default)
    {
        _covers.Add(cover.Id, cover);
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_covers.Remove(id));
}
