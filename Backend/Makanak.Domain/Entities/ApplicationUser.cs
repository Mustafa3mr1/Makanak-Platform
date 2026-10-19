using Microsoft.AspNetCore.Identity;
using Makanak.Domain.Enums;

namespace Makanak.Domain.Entities;

/// <summary>
/// Application user extending IdentityUser with a Guid primary key, enterprise roles, and domain relationships.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;

    public UserType UserType { get; set; } = UserType.Seeker;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual ICollection<Property> OwnedProperties { get; set; } = new List<Property>();

    public virtual ICollection<Offer> SubmittedOffers { get; set; } = new List<Offer>();
}
