using Makanak.Domain.Enums;

namespace Makanak.Application.DTOs.Auth;

public record RegisterRequestDto(
    string FullName,
    string Email,
    string Password,
    UserType UserType
);

public record LoginRequestDto(
    string Email,
    string Password
);

public record AuthResponseDto(
    Guid UserId,
    string FullName,
    string Email,
    UserType UserType,
    string Token,
    DateTime ExpiresAtUtc
);
