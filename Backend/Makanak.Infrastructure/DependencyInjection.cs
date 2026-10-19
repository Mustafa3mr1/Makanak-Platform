using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Makanak.Application.Common.Interfaces;
using Makanak.Domain.Entities;
using Makanak.Infrastructure.Data;
using Makanak.Infrastructure.Repositories;
using Makanak.Infrastructure.Services;

namespace Makanak.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Server=(localdb)\\mssqllocaldb;Database=MakanakDb;Trusted_Connection=True;MultipleActiveResultSets=true";

        // EF Core with SQL Server + NetTopologySuite for spatial queries
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, x => x.UseNetTopologySuite()));

        // ASP.NET Core Identity with Guid PK
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequiredLength = 6;
        })
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<ApplicationDbContext>();

        // Repositories & Unit of Work
        services.AddScoped<IPropertyRepository, PropertyRepository>();
        services.AddScoped<IOfferRepository, OfferRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // SignalR & Domain Services
        services.AddSignalR();
        services.AddScoped<ISignalRNotificationService, SignalRNotificationService>();
        services.AddScoped<INegotiationService, NegotiationService>();
        services.AddScoped<IPropertyService, PropertyService>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
