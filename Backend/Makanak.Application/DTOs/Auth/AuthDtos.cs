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

/// <summary>
/// Auth response DTO. Exposes <see cref="UserId"/> as a Guid (PublicId).
/// The internal long Id is NEVER returned to the client.
/// </summary>
public record AuthResponseDto(
    Guid UserId,       // PublicId — the client's permanent external reference
    string FullName,
    string Email,
    UserType UserType,
    string Token,      // Signed JWT
    DateTime ExpiresAtUtc
);
