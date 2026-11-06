using Claims.Application.Repositories;
using Claims.Domain;
using Microsoft.EntityFrameworkCore;

namespace Claims.Infrastructure.Persistence;

public class CoverRepository(ClaimsContext context) : ICoverRepository
{
    public async Task<IReadOnlyList<Cover>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.Covers.ToListAsync(cancellationToken);

    public Task<Cover?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        context.Covers.SingleOrDefaultAsync(cover => cover.Id == id, cancellationToken);

    public async Task AddAsync(Cover cover, CancellationToken cancellationToken = default)
    {
        context.Covers.Add(cover);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var cover = await GetByIdAsync(id, cancellationToken);
        if (cover is null)
        {
            return false;
        }

        context.Covers.Remove(cover);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
