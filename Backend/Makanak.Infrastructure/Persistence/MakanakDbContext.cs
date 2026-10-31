using Makanak.Domain.Entities;
using Makanak.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Makanak.Infrastructure.Persistence;

public class MakanakDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public MakanakDbContext(DbContextOptions<MakanakDbContext> options) : base(options) { }

    public DbSet<Property> Properties => Set<Property>();
    public DbSet<PropertyImage> PropertyImages => Set<PropertyImage>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<NegotiationHistory> NegotiationHistories => Set<NegotiationHistory>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(MakanakDbContext).Assembly);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyRules();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyRules();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }


    private void ApplyRules()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<NegotiationHistory>())
        {
            if (entry.State is EntityState.Modified or EntityState.Deleted)
                throw new InvalidOperationException("Negotiation history is append-only and cannot be modified or deleted.");
        }

        foreach (var entry in ChangeTracker.Entries<Property>().Where(e => e.State == EntityState.Modified))
            entry.Entity.UpdatedAtUtc = now;

        foreach (var entry in ChangeTracker.Entries<Offer>().Where(e => e.State == EntityState.Modified))
            entry.Entity.UpdatedAtUtc = now;
    }
}
