using Makanak.Application.Common.Interfaces;
using Makanak.Infrastructure.Authentication;
using Makanak.Infrastructure.Identity;
using Makanak.Infrastructure.Persistence;
using Makanak.Infrastructure.Repositories;
using Makanak.Infrastructure.Storage;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Makanak.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, string contentRootPath)
    {
        // Connection string is read lazily so test hosts can override configuration.
        services.AddDbContext<MakanakDbContext>((sp, options) =>
        {
            var cs = sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")
                     ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");
            options.UseSqlServer(cs, sql => sql.MigrationsAssembly(typeof(MakanakDbContext).Assembly.FullName));
        });

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<MakanakDbContext>();

        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<ImageStorageSettings>()
            .Bind(configuration.GetSection(ImageStorageSettings.SectionName))
            .PostConfigure(s =>
            {
                s.RootPath = Path.GetFullPath(Path.IsPathRooted(s.RootPath) ? s.RootPath : Path.Combine(contentRootPath, s.RootPath));
            });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserAccountGateway, UserAccountGateway>();
        services.AddSingleton<IJwtService, JwtService>();
        services.AddSingleton<IImageStorageService, LocalImageStorageService>();

        return services;
    }
}
