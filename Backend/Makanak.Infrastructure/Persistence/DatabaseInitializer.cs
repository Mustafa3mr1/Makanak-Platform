using Makanak.Application.Common.Models;
using Makanak.Domain.Enums;
using Makanak.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Makanak.Infrastructure.Persistence;

public static class DatabaseInitializer
{

    public static async Task MigrateAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MakanakDbContext>();
        await db.Database.MigrateAsync(ct);
    }

    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in RoleNames.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var created = await roleManager.CreateAsync(new IdentityRole<Guid>(role) { Id = Guid.NewGuid() });
                if (!created.Succeeded)
                    throw new InvalidOperationException($"Could not create role '{role}': {string.Join("; ", created.Errors.Select(e => e.Description))}");
            }
        }

        var email = configuration["Admin:Email"];
        var password = configuration["Admin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation("Admin:Email / Admin:Password not configured; no initial admin provisioned.");
            return;
        }

        if (await userManager.FindByEmailAsync(email) is not null)
        {
            logger.LogInformation("Initial admin already exists.");
            return;
        }

        var admin = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = string.IsNullOrWhiteSpace(configuration["Admin:FullName"]) ? "Platform Administrator" : configuration["Admin:FullName"]!,
            UserType = UserType.Admin,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(admin, password);
        if (!result.Succeeded)
            throw new InvalidOperationException($"Could not create initial admin: {string.Join("; ", result.Errors.Select(e => e.Description))}");

        var roleResult = await userManager.AddToRoleAsync(admin, RoleNames.Admin);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(admin);
            throw new InvalidOperationException($"Could not assign Admin role: {string.Join("; ", roleResult.Errors.Select(e => e.Description))}");
        }

        logger.LogInformation("Initial admin account created for {Email}.", email);
    }
}
