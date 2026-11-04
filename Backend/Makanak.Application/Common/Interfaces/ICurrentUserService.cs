namespace Makanak.Application.Common.Interfaces;

/// <summary>
/// Provides the authenticated user's identity extracted from JWT claims.
/// Implemented in the Infrastructure/API layer; consumed by Application services.
/// Prevents services from depending on HttpContext directly.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// The authenticated user's internal long Id (resolved from JWT sub claim).
    /// Throws <see cref="UnauthorizedAccessException"/> if the user is not authenticated.
    /// </summary>
    long GetCurrentUserId();

    /// <summary>
    /// The authenticated user's public Guid (resolved from JWT claims).
    /// Used for outbound API responses.
    /// </summary>
    Guid GetCurrentUserPublicId();

    /// <summary>Returns true if there is an authenticated user in the current request context.</summary>
    bool IsAuthenticated { get; }
}

