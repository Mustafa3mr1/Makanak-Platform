using Makanak.Domain.Enums;

namespace Makanak.Application.DTOs.Auth;


public sealed record RegisterRequest(string FullName, string Email, string Password, UserType UserType);

public sealed record LoginRequest(string Email, string Password);

public sealed record AuthResponse(string AccessToken, string TokenType, DateTime ExpiresAtUtc, DTOs.User.UserProfileDto User);
