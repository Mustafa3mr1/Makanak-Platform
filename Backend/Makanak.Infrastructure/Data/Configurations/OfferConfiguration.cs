using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Makanak.Domain.Entities;

namespace Makanak.Infrastructure.Data.Configurations;

public sealed class OfferConfiguration : IEntityTypeConfiguration<Offer>
{
    public void Configure(EntityTypeBuilder<Offer> builder)
    {
        // ── Primary Key (long — clustered) ───────────────────────────────────────
        builder.HasKey(o => o.Id);

        // ── PublicId: unique non-clustered index ─────────────────────────────────
        builder.HasIndex(o => o.PublicId)
               .IsUnique()
               .HasDatabaseName("IX_Offers_PublicId");

        builder.Property(o => o.PublicId).IsRequired();

        // ── Columns ──────────────────────────────────────────────────────────────
        builder.Property(o => o.CurrentPrice).HasPrecision(18, 2).IsRequired();
        builder.Property(o => o.Status).IsRequired();
        builder.Property(o => o.RoundCount).IsRequired();

        // NFR-03: RowVersion for optimistic concurrency
        builder.Property(o => o.RowVersion).IsRowVersion();

        // ── Relationships ─────────────────────────────────────────────────────────
        builder.HasOne(o => o.Property)
               .WithMany(p => p.Offers)
               .HasForeignKey(o => o.PropertyId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(o => o.Seeker)
               .WithMany(u => u.SubmittedOffers)
               .HasForeignKey(o => o.SeekerId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

