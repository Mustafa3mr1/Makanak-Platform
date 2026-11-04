using Makanak.Domain.Enums;
using Makanak.Domain.Exceptions;

namespace Makanak.Domain.Entities;

/// <summary>
/// Core real estate property aggregate root.
/// Uses Hybrid ID: <see cref="BaseEntity.Id"/> (internal clustered PK) + <see cref="BaseEntity.PublicId"/> (API surface).
/// Location uses string fields for MVP scope (no NetTopologySuite).
/// Optimistic concurrency enforced via RowVersion (NFR-03).
/// All state mutations are gated by domain methods — public setters are intentionally absent.
/// </summary>
public class Property : BaseEntity
{
    // ── Internal FK (long) pointing to ApplicationUser.Id ───────────────────────
    public long OwnerId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public decimal Price { get; private set; }

    public PropertyStatus Status { get; private set; } = PropertyStatus.Available;

    /// <summary>City where the property is located (replaces geospatial Point – MVP scope).</summary>
    public string City { get; private set; } = string.Empty;

    /// <summary>Full street address of the property.</summary>
    public string Address { get; private set; } = string.Empty;

    /// <summary>Concurrency token for optimistic locking (NFR-03).</summary>
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; private set; }

    // Navigation Properties
    public virtual ApplicationUser Owner { get; private set; } = null!;
    public virtual ICollection<Offer> Offers { get; private set; } = new List<Offer>();
    public virtual ICollection<PropertyImage> Images { get; private set; } = new List<PropertyImage>();

    // ── Factory ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Domain factory method — the only valid way to create a new Property aggregate.
    /// Enforces that price must be positive at the point of creation.
    /// </summary>
    public static Property Create(long ownerId, string title, string description, decimal price, string city, string address)
    {
        if (price <= 0)
            throw new InvalidPropertyOperationException("Property price must be greater than zero.");
        if (string.IsNullOrWhiteSpace(title))
            throw new InvalidPropertyOperationException("Property title is required.");
        if (string.IsNullOrWhiteSpace(city))
            throw new InvalidPropertyOperationException("Property city is required.");

        var property = new Property
        {
            OwnerId = ownerId,
            Title = title.Trim(),
            Description = description.Trim(),
            Price = price,
            City = city.Trim(),
            Address = address.Trim()
        };
        return property;
    }

    // ── Domain Methods ───────────────────────────────────────────────────────────

    public void MarkAsUnavailable()
    {
        Status = PropertyStatus.Unavailable;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkAsSold()
    {
        Status = PropertyStatus.Sold;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkAsRented()
    {
        Status = PropertyStatus.Rented;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkAsAvailable()
    {
        Status = PropertyStatus.Available;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void EnsureIsAvailable()
    {
        if (Status != PropertyStatus.Available)
            throw new PropertyUnavailableException($"Property '{PublicId}' is not available. Current status: {Status}");
    }

    // EF Core requires parameterless constructor for hydration.
    private Property() { }
}
