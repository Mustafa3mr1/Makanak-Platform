using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Makanak.Domain.Entities;

namespace Makanak.Infrastructure.Data;

/// <summary>
/// EF Core DbContext.
/// ApplicationUser now uses <c>long</c> as the IdentityUser PK for clustered index performance.
/// All entity configurations are in separate IEntityTypeConfiguration classes.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<long>, long>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Property> Properties => Set<Property>();
    public DbSet<PropertyImage> PropertyImages => Set<PropertyImage>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<NegotiationHistory> NegotiationHistories => Set<NegotiationHistory>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply all IEntityTypeConfiguration<T> classes in this assembly automatically.
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
