using System.Security.Claims;
using Makanak.Application.Common.Exceptions;

namespace Makanak.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>The authenticated user's id, taken only from the validated JWT ("sub"). Never from request bodies.</summary>
    public static Guid GetUserId(this ClaimsPrincipal user)
        => user.TryGetUserId() ?? throw new UnauthorizedException("Invalid or missing user identity.");

    public static Guid? TryGetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
