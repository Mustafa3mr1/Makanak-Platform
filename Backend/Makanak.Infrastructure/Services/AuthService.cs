using Microsoft.AspNetCore.Identity;
using Makanak.Application.Common.Interfaces;
using Makanak.Application.DTOs.Auth;
using Makanak.Domain.Entities;
using Makanak.Domain.Exceptions;

namespace Makanak.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AuthService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new InvalidOfferOperationException("User with this email already exists.");
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            UserType = request.UserType
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOfferOperationException($"Failed to register user: {errors}");
        }

        // Return token placeholder (can be expanded to JWT handler)
        var token = $"demo_token_{Guid.NewGuid()}";
        return new AuthResponseDto(user.Id, user.FullName, user.Email!, user.UserType, token, DateTime.UtcNow.AddDays(7));
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email)
            ?? throw new InvalidOfferOperationException("Invalid email or password.");

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            throw new InvalidOfferOperationException("Invalid email or password.");
        }

        var token = $"demo_token_{Guid.NewGuid()}";
        return new AuthResponseDto(user.Id, user.FullName, user.Email!, user.UserType, token, DateTime.UtcNow.AddDays(7));
    }
}
