using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Makanak.Domain.Entities;

namespace Makanak.Infrastructure.Identity;

/// <summary>
/// Generates signed JWT tokens for authenticated users.
/// Embeds both the internal long Id (sub claim) and the PublicId (public_id claim)
/// so the API layer can resolve the user's identity from claims without hitting the DB.
/// </summary>
public sealed class JwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration) => _configuration = configuration;

    public (string token, DateTime expiresAtUtc) GenerateToken(ApplicationUser user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secretKey = jwtSettings["Secret"]
            ?? throw new InvalidOperationException("JWT Secret is not configured. Set Jwt:Secret in user secrets or environment variables.");

        var issuer = jwtSettings["Issuer"] ?? "makanak-api";
        var audience = jwtSettings["Audience"] ?? "makanak-clients";
        var expiryMinutes = int.TryParse(jwtSettings["ExpiryMinutes"], out var minutes) ? minutes : 60;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var claims = new[]
        {
            // sub (NameIdentifier): internal long Id — used by CurrentUserService.GetCurrentUserId()
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            // public_id: Guid PublicId — used by CurrentUserService.GetCurrentUserPublicId()
            new Claim("public_id", user.PublicId.ToString()),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim("user_type", user.UserType.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: credentials
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}

