using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Makanak.Domain.Entities;

namespace Makanak.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Offer> Offers => Set<Offer>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ApplicationUser configuration
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.FullName).HasMaxLength(150).IsRequired();
            entity.Property(u => u.UserType).IsRequired();
        });

        // Property configuration
        builder.Entity<Property>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Title).HasMaxLength(200).IsRequired();
            entity.Property(p => p.Price).HasPrecision(18, 2).IsRequired();
            entity.Property(p => p.Status).IsRequired();

            // Geospatial column with SRID 4326
            entity.Property(p => p.Location)
                  .HasColumnType("geography")
                  .IsRequired();

            // Spatial index for high performance proximity searches
            entity.HasIndex(p => p.Location);

            // RowVersion for optimistic concurrency control
            entity.Property(p => p.RowVersion)
                  .IsRowVersion();

            entity.HasOne(p => p.Owner)
                  .WithMany(u => u.OwnedProperties)
                  .HasForeignKey(p => p.OwnerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Offer configuration
        builder.Entity<Offer>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.CurrentPrice).HasPrecision(18, 2).IsRequired();
            entity.Property(o => o.Status).IsRequired();
            entity.Property(o => o.RoundCount).IsRequired();

            // RowVersion for optimistic concurrency control
            entity.Property(o => o.RowVersion)
                  .IsRowVersion();

            entity.HasOne(o => o.Property)
                  .WithMany(p => p.Offers)
                  .HasForeignKey(o => o.PropertyId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(o => o.Seeker)
                  .WithMany(u => u.SubmittedOffers)
                  .HasForeignKey(o => o.SeekerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
