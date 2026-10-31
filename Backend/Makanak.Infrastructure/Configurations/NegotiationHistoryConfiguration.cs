using Makanak.Domain.Entities;
using Makanak.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Makanak.Infrastructure.Configurations;

public class NegotiationHistoryConfiguration : IEntityTypeConfiguration<NegotiationHistory>
{
    public void Configure(EntityTypeBuilder<NegotiationHistory> builder)
    {
        builder.ToTable("NegotiationHistories");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.ActionType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(h => h.ProposedPrice).HasPrecision(18, 2);
        builder.Property(h => h.Timestamp).IsRequired();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(h => h.ActionByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(h => new { h.OfferId, h.Timestamp });
        builder.HasIndex(h => h.ActionByUserId);
    }
}
