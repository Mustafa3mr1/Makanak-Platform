using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Makanak.Application.Common.Interfaces;

namespace Makanak.Infrastructure.Identity;

/// <summary>
/// Resolves the authenticated user's identity from JWT claims via IHttpContextAccessor.
/// Registered as Scoped — one instance per HTTP request.
///
/// JWT Claims used:
///   - ClaimTypes.NameIdentifier ("sub") → internal long Id
///   - "public_id" (custom claim)        → Guid PublicId
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    private ClaimsPrincipal? Principal
        => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated
        => Principal?.Identity?.IsAuthenticated ?? false;

    public long GetCurrentUserId()
    {
        var value = Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(value) || !long.TryParse(value, out var id))
            throw new UnauthorizedAccessException("Authenticated user identity could not be resolved from token.");
        return id;
    }

    public Guid GetCurrentUserPublicId()
    {
        var value = Principal?.FindFirstValue("public_id");
        if (string.IsNullOrWhiteSpace(value) || !Guid.TryParse(value, out var publicId))
            throw new UnauthorizedAccessException("User public identifier could not be resolved from token.");
        return publicId;
    }
}

