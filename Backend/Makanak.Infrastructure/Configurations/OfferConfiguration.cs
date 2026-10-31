using Makanak.Domain.Entities;
using Makanak.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Makanak.Infrastructure.Configurations;

public class OfferConfiguration : IEntityTypeConfiguration<Offer>
{
    public void Configure(EntityTypeBuilder<Offer> builder)
    {
        builder.ToTable("Offers", t =>
        {
            t.HasCheckConstraint("CK_Offers_CurrentPrice", "[CurrentPrice] > 0");
            t.HasCheckConstraint("CK_Offers_RoundCount", "[RoundCount] >= 1");
        });
        builder.HasKey(o => o.Id);

        builder.Property(o => o.CurrentPrice).HasPrecision(18, 2);
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(o => o.RoundCount).IsRequired();
        builder.Property(o => o.RowVersion).IsRowVersion();
        builder.Property(o => o.CreatedAtUtc).IsRequired();
        builder.Property(o => o.UpdatedAtUtc).IsRequired();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(o => o.SeekerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(o => o.History)
            .WithOne(h => h.Offer)
            .HasForeignKey(h => h.OfferId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(o => new { o.PropertyId, o.Status });
        builder.HasIndex(o => new { o.SeekerId, o.Status });
        builder.HasIndex(o => o.UpdatedAtUtc);

        // A seeker may have only one OPEN negotiation per property (also guards against double-click / race duplicates).
        builder.HasIndex(o => new { o.PropertyId, o.SeekerId })
            .IsUnique()
            .HasFilter("[Status] IN (N'PendingOwnerResponse', N'PendingBuyerResponse')")
            .HasDatabaseName("UX_Offers_OneOpenOfferPerSeekerPerProperty");
    }
}
