using Makanak.Domain.Entities;

namespace Makanak.Application.Common.Interfaces;

/// <summary>
/// Repository for <see cref="ApplicationUser"/> lookups needed by application services.
/// </summary>
public interface IUserRepository
{
    /// <summary>Finds a user by their public Guid identifier (safe for API exposure).</summary>
    Task<ApplicationUser?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default);

    /// <summary>Finds a user by their internal long Id (for FK resolution only — never expose externally).</summary>
    Task<ApplicationUser?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
}

