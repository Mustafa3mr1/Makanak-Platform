using Microsoft.AspNetCore.Identity;
using Makanak.Domain.Enums;

namespace Makanak.Domain.Entities;

/// <summary>
/// Application user extending IdentityUser&lt;long&gt; with a long primary key for clustering.
/// PublicId (Guid) is exposed via the API surface; the numeric Id is used for all FK joins.
/// </summary>
public class ApplicationUser : IdentityUser<long>
{
    public Guid PublicId { get; private set; } = Guid.NewGuid();

    public string FullName { get; private set; } = string.Empty;

    public UserType UserType { get; private set; } = UserType.Seeker;

    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual ICollection<Property> OwnedProperties { get; private set; } = new List<Property>();
    public virtual ICollection<Offer> SubmittedOffers { get; private set; } = new List<Offer>();

    // ── Factory ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Domain factory for creating a new ApplicationUser.
    /// ASP.NET Core Identity will assign the numeric Id on insert.
    /// </summary>
    public static ApplicationUser Create(string email, string fullName, UserType userType)
    {
        return new ApplicationUser
        {
            UserName = email,
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant(),
            FullName = fullName,
            UserType = userType
        };
    }

    // EF Core parameterless constructor.
    private ApplicationUser() { }
}
