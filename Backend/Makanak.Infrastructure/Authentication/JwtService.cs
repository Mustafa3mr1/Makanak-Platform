using System.Security.Claims;
using System.Text;
using Makanak.Application.Common.Interfaces;
using Makanak.Application.Common.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Makanak.Infrastructure.Authentication;

public sealed class JwtService : IJwtService
{
    private readonly JwtSettings _settings;

    public JwtService(IOptions<JwtSettings> options) => _settings = options.Value;

    public JwtTokenResult CreateToken(UserAccountInfo user)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_settings.ExpiryMinutes);

        var claims = new List<Claim>
        {
            new("sub", user.Id.ToString()),
            new("email", user.Email),
            new("name", user.FullName),
            new("role", RoleNames.For(user.UserType)),
            new("jti", Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey));
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            NotBefore = now,
            Expires = expires,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return new JwtTokenResult(token, expires);
    }
}
