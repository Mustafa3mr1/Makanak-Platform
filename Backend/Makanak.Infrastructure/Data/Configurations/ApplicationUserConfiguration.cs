using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Makanak.Domain.Entities;

namespace Makanak.Infrastructure.Data.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        // ApplicationUser.Id is long (IdentityUser<long>) — clustered PK by convention.

        // PublicId: unique non-clustered index; exposed externally via API.
        builder.HasIndex(u => u.PublicId)
               .IsUnique()
               .HasDatabaseName("IX_AspNetUsers_PublicId");

        builder.Property(u => u.PublicId)
               .IsRequired();

        builder.Property(u => u.FullName)
               .HasMaxLength(150)
               .IsRequired();

        builder.Property(u => u.UserType)
               .IsRequired();

        builder.Property(u => u.CreatedAtUtc)
               .IsRequired();
    }
}

