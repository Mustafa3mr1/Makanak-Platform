using NetTopologySuite.Geometries;
using Makanak.Domain.Enums;
using Makanak.Domain.Exceptions;

namespace Makanak.Domain.Entities;

/// <summary>
/// Core real estate property entity with geospatial spatial coordinates and optimistic concurrency.
/// </summary>
public class Property
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OwnerId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public PropertyStatus Status { get; set; } = PropertyStatus.Available;

    /// <summary>
    /// Geospatial coordinates (Longitude, Latitude) stored as a Point with SRID 4326 (WGS 84).
    /// </summary>
    public Point Location { get; set; } = null!;

    /// <summary>
    /// Concurrency token for optimistic locking during concurrent offer operations.
    /// </summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    // Navigation Properties
    public virtual ApplicationUser Owner { get; set; } = null!;

    public virtual ICollection<Offer> Offers { get; set; } = new List<Offer>();

    // Domain Methods
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
        {
            throw new PropertyUnavailableException($"Property '{Id}' is not available for negotiation. Current status: {Status}");
        }
    }
}
