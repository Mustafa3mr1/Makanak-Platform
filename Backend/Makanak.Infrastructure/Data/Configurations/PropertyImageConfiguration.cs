using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Makanak.Domain.Entities;

namespace Makanak.Infrastructure.Data.Configurations;

public sealed class PropertyImageConfiguration : IEntityTypeConfiguration<PropertyImage>
{
    public void Configure(EntityTypeBuilder<PropertyImage> builder)
    {
        builder.HasKey(pi => pi.Id);

        builder.HasIndex(pi => pi.PublicId)
               .IsUnique()
               .HasDatabaseName("IX_PropertyImages_PublicId");

        builder.Property(pi => pi.PublicId).IsRequired();
        builder.Property(pi => pi.ImageUrl).HasMaxLength(2048).IsRequired();
        builder.Property(pi => pi.IsPrimary).IsRequired();

        builder.HasOne(pi => pi.Property)
               .WithMany(p => p.Images)
               .HasForeignKey(pi => pi.PropertyId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

