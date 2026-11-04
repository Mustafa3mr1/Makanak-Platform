using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Makanak.Domain.Entities;

namespace Makanak.Infrastructure.Data.Configurations;

public sealed class NegotiationHistoryConfiguration : IEntityTypeConfiguration<NegotiationHistory>
{
    public void Configure(EntityTypeBuilder<NegotiationHistory> builder)
    {
        // NegotiationHistory uses Guid PK (not BaseEntity) — append-only audit log.
        builder.HasKey(nh => nh.Id);

        builder.Property(nh => nh.ActionType).IsRequired();
        builder.Property(nh => nh.ProposedPrice).HasPrecision(18, 2).IsRequired();
        builder.Property(nh => nh.Timestamp).IsRequired();

        // Immutable audit log — no cascade delete from Offer
        builder.HasOne(nh => nh.Offer)
               .WithMany()
               .HasForeignKey(nh => nh.OfferId)
               .OnDelete(DeleteBehavior.Restrict);

        // Actor reference — no cascade delete from ApplicationUser
        builder.HasOne(nh => nh.ActionByUser)
               .WithMany()
               .HasForeignKey(nh => nh.ActionByUserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

