using Microsoft.AspNetCore.Identity;
using Makanak.Application.Common.Interfaces;
using Makanak.Application.DTOs.Auth;
using Makanak.Domain.Entities;
using Makanak.Domain.Exceptions;
using Makanak.Infrastructure.Identity;

namespace Makanak.Infrastructure.Services;

/// <summary>
/// Authentication service. Issues real signed JWTs via JwtTokenService.
/// No DbContext injection — uses ASP.NET Core Identity's UserManager only.
/// Throws <see cref="AuthException"/> for auth failures (not InvalidOfferOperationException).
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtTokenService _jwtTokenService;

    public AuthService(UserManager<ApplicationUser> userManager, JwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
            throw new AuthException($"An account with email '{request.Email}' already exists.");

        var user = ApplicationUser.Create(request.Email, request.FullName, request.UserType);

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new AuthException($"Registration failed: {errors}");
        }

        var (token, expiresAt) = _jwtTokenService.GenerateToken(user);
        return new AuthResponseDto(user.PublicId, user.FullName, user.Email!, user.UserType, token, expiresAt);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        // Generic message prevents user enumeration attacks
        const string invalidCredentialsMessage = "Invalid email or password.";

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
            throw new AuthException(invalidCredentialsMessage);

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
            throw new AuthException(invalidCredentialsMessage);

        var (token, expiresAt) = _jwtTokenService.GenerateToken(user);
        return new AuthResponseDto(user.PublicId, user.FullName, user.Email!, user.UserType, token, expiresAt);
    }
}
