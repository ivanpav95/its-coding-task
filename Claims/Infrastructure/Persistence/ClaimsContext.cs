using Claims.Domain;
using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;

namespace Claims.Infrastructure.Persistence;

public class ClaimsContext(DbContextOptions<ClaimsContext> options) : DbContext(options)
{
    public DbSet<Claim> Claims => Set<Claim>();

    public DbSet<Cover> Covers => Set<Cover>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Claim>().ToCollection("claims");
        modelBuilder.Entity<Cover>().ToCollection("covers");
    }
}
