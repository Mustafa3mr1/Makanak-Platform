using System.ComponentModel.DataAnnotations;

namespace Makanak.Infrastructure.Authentication;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    [Required, MinLength(1)]
    public string Issuer { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public string Audience { get; set; } = string.Empty;

    [Required, MinLength(32, ErrorMessage = "Jwt:SigningKey must be at least 32 characters. Set it with user-secrets or an environment variable.")]
    public string SigningKey { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int ExpiryMinutes { get; set; } = 60;
}
