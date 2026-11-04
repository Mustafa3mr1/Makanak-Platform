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
        builder.Property(p => p.Description).HasMaxLength(2000);
        builder.Property(p => p.Price).HasPrecision(18, 2).IsRequired();
        builder.Property(p => p.Status).IsRequired();

        // MVP: standard string location fields
        builder.Property(p => p.City).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Address).HasMaxLength(500).IsRequired();

        // NFR-03: RowVersion for optimistic concurrency
        builder.Property(p => p.RowVersion).IsRowVersion();

        // ── Relationships ─────────────────────────────────────────────────────────
        builder.HasOne(p => p.Owner)
               .WithMany(u => u.OwnedProperties)
               .HasForeignKey(p => p.OwnerId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

