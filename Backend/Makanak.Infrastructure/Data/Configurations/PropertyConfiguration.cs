using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Makanak.Domain.Entities;

namespace Makanak.Infrastructure.Data.Configurations;

public sealed class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        // ── Primary Key (long — clustered) ───────────────────────────────────────
        builder.HasKey(p => p.Id);

        // ── PublicId: unique non-clustered index ─────────────────────────────────
        builder.HasIndex(p => p.PublicId)
               .IsUnique()
               .HasDatabaseName("IX_Properties_PublicId");

        builder.Property(p => p.PublicId).IsRequired();

        // ── Columns ──────────────────────────────────────────────────────────────
        builder.Property(p => p.Title).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(4000);
        builder.Property(p => p.Price).HasPrecision(18, 2).IsRequired();
        builder.Property(p => p.PropertyType).IsRequired();
        builder.Property(p => p.ListingType).IsRequired();
        builder.Property(p => p.Status).IsRequired();

        builder.Property(p => p.City).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Address).HasMaxLength(250).IsRequired();

        builder.Property(p => p.Bedrooms).IsRequired();
        builder.Property(p => p.Bathrooms).IsRequired();
        builder.Property(p => p.AreaSqm).HasPrecision(18, 2).IsRequired();

        builder.Property(p => p.Latitude).HasPrecision(9, 6);
        builder.Property(p => p.Longitude).HasPrecision(9, 6);

        builder.Property(p => p.Features).IsRequired();

        builder.Property(p => p.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(p => p.DeletedAt);

        // NFR-03: RowVersion for optimistic concurrency
        builder.Property(p => p.RowVersion).IsRowVersion();

        // ── Global Query Filter for Soft Delete ──────────────────────────────────
        builder.HasQueryFilter(p => !p.IsDeleted);

        // ── Relationships ─────────────────────────────────────────────────────────
        builder.HasOne(p => p.Owner)
               .WithMany(u => u.OwnedProperties)
               .HasForeignKey(p => p.OwnerId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Images)
               .WithOne(i => i.Property)
               .HasForeignKey(i => i.PropertyId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
