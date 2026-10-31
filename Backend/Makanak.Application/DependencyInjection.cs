using FluentValidation;
using Makanak.Application.Common.Interfaces;
using Makanak.Application.Services;
using Makanak.Application.Validators;
using Microsoft.Extensions.DependencyInjection;

namespace Makanak.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>(ServiceLifetime.Singleton);



        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPropertyService, PropertyService>();
        services.AddScoped<IOfferService, OfferService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAdminService, AdminService>();
        return services;
    }
}

public sealed class BasicPagedQueryValidator : PagedQueryValidator<Common.Models.PagedQuery> { }
