using Microsoft.AspNetCore.Identity;
using Makanak.Application.Common.Interfaces;
using Makanak.Application.DTOs.Auth;
using Makanak.Domain.Entities;
using Makanak.Domain.Exceptions;

namespace Makanak.Infrastructure.Services;

public sealed class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<UserProfileDto> GetCurrentUserProfileAsync(long userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException($"User with Id '{userId}' not found.");

        return new UserProfileDto(
            user.PublicId,
            user.FullName,
            user.Email!,
            user.UserType,
            user.PhoneNumber);
    }

    public async Task<UserProfileDto> UpdateUserProfileAsync(
        long userId,
        UpdateUserProfileRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException($"User with Id '{userId}' not found.");

        user.UpdateProfile(request.FullName.Trim(), request.PhoneNumber?.Trim());

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new DomainException($"Failed to update profile: {errors}", "validation_failed");
        }

        return new UserProfileDto(
            user.PublicId,
            user.FullName,
            user.Email!,
            user.UserType,
            user.PhoneNumber);
    }
}
