using Makanak.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Makanak.Infrastructure.Configurations;

public class PropertyImageConfiguration : IEntityTypeConfiguration<PropertyImage>
{
    public void Configure(EntityTypeBuilder<PropertyImage> builder)
    {
        builder.ToTable("PropertyImages");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.ImageUrl).IsRequired().HasMaxLength(500);
        builder.Property(i => i.IsPrimary).IsRequired();

        builder.HasIndex(i => i.PropertyId);

        // At most ONE primary image per property, enforced by the database (SQL Server filtered unique index).
        builder.HasIndex(i => i.PropertyId)
            .IsUnique()
            .HasFilter("[IsPrimary] = 1")
            .HasDatabaseName("UX_PropertyImages_OnePrimaryPerProperty");
    }
}
