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

        // Element names match the documents written by the original template,
        // including "claimType" for Cover.Type.
        modelBuilder.Entity<Claim>(claim =>
        {
            claim.ToCollection("claims");
            claim.Property(c => c.CoverId).HasElementName("coverId");
            claim.Property(c => c.Created).HasElementName("created");
            claim.Property(c => c.Name).HasElementName("name");
            claim.Property(c => c.Type).HasElementName("claimType");
            claim.Property(c => c.DamageCost).HasElementName("damageCost");
        });

        modelBuilder.Entity<Cover>(cover =>
        {
            cover.ToCollection("covers");
            cover.Property(c => c.StartDate).HasElementName("startDate");
            cover.Property(c => c.EndDate).HasElementName("endDate");
            cover.Property(c => c.Type).HasElementName("claimType");
            cover.Property(c => c.Premium).HasElementName("premium");
        });
    }
}
