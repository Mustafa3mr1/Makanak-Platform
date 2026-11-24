using Makanak.Domain.Enums;

namespace Makanak.Application.DTOs.Auth;

public record RegisterRequestDto(
    string FullName,
    string Email,
    string Password,
    UserType UserType,
    string? PhoneNumber = null
);

public record LoginRequestDto(
    string Email,
    string Password
);

public record UserProfileDto(
    Guid PublicId,
    string FullName,
    string Email,
    UserType UserType,
    string? PhoneNumber
);

public record AuthResponseDto(
    string Token,
    DateTime ExpiresAt,
    UserProfileDto User
);

public record UpdateUserProfileRequestDto(
    string FullName,
    string? PhoneNumber
);
