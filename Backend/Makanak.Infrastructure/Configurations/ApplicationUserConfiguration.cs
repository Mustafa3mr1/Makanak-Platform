using Makanak.Domain.Enums;
using Makanak.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Makanak.Infrastructure.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FullName).IsRequired().HasMaxLength(200);
        builder.Property(u => u.UserType).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(u => u.CreatedAtUtc).IsRequired();
        builder.Property(u => u.IsActive).IsRequired().HasDefaultValue(true);
        builder.HasIndex(u => u.NormalizedEmail).IsUnique().HasDatabaseName("UX_AspNetUsers_NormalizedEmail")
            .HasFilter("[NormalizedEmail] IS NOT NULL");
        builder.HasIndex(u => u.UserType);
    }
}
