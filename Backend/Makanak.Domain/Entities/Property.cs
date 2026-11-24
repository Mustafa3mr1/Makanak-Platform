using Makanak.Domain.Enums;
using Makanak.Domain.Exceptions;

namespace Makanak.Domain.Entities;

/// <summary>
/// Core real estate property aggregate root.
/// Uses Hybrid ID: <see cref="BaseEntity.Id"/> (internal clustered PK) + <see cref="BaseEntity.PublicId"/> (API surface).
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

    public PropertyType PropertyType { get; private set; } = PropertyType.Apartment;

    public ListingType ListingType { get; private set; } = ListingType.Sale;

    public PropertyStatus Status { get; private set; } = PropertyStatus.Available;

    public string City { get; private set; } = string.Empty;

    public string Address { get; private set; } = string.Empty;

    public int Bedrooms { get; private set; }

    public int Bathrooms { get; private set; }

    public decimal AreaSqm { get; private set; }

    public decimal? Latitude { get; private set; }

    public decimal? Longitude { get; private set; }

    public Features Features { get; private set; } = Features.None;

    public bool IsDeleted { get; private set; }

    public DateTime? DeletedAt { get; private set; }

    /// <summary>Concurrency token for optimistic locking (NFR-03).</summary>
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    // Navigation Properties
    public virtual ApplicationUser Owner { get; private set; } = null!;
    public virtual ICollection<Offer> Offers { get; private set; } = new List<Offer>();
    public virtual ICollection<PropertyImage> Images { get; private set; } = new List<PropertyImage>();

    // ── Factory ──────────────────────────────────────────────────────────────────

    public static Property Create(
        long ownerId,
        string title,
        string description,
        decimal price,
        PropertyType propertyType,
        ListingType listingType,
        string city,
        string address,
        int bedrooms,
        int bathrooms,
        decimal areaSqm,
        decimal? latitude = null,
        decimal? longitude = null,
        Features features = Features.None)
    {
        if (price <= 0)
            throw new InvalidPropertyOperationException("Property price must be greater than zero.");
        if (string.IsNullOrWhiteSpace(title))
            throw new InvalidPropertyOperationException("Property title is required.");
        if (string.IsNullOrWhiteSpace(city))
            throw new InvalidPropertyOperationException("Property city is required.");
        if (string.IsNullOrWhiteSpace(address))
            throw new InvalidPropertyOperationException("Property address is required.");
        if (areaSqm <= 0)
            throw new InvalidPropertyOperationException("Property area in square meters must be greater than zero.");
        if (bedrooms < 0)
            throw new InvalidPropertyOperationException("Bedrooms cannot be negative.");
        if (bathrooms < 0)
            throw new InvalidPropertyOperationException("Bathrooms cannot be negative.");

        var property = new Property
        {
            OwnerId = ownerId,
            Title = title.Trim(),
            Description = description?.Trim() ?? string.Empty,
            Price = price,
            PropertyType = propertyType,
            ListingType = listingType,
            Status = PropertyStatus.Available,
            City = city.Trim(),
            Address = address.Trim(),
            Bedrooms = bedrooms,
            Bathrooms = bathrooms,
            AreaSqm = areaSqm,
            Latitude = latitude,
            Longitude = longitude,
            Features = features,
            IsDeleted = false
        };

        return property;
    }

    // ── Domain Methods ───────────────────────────────────────────────────────────

    public void Update(
        string title,
        string description,
        decimal price,
        PropertyType propertyType,
        ListingType listingType,
        string city,
        string address,
        int bedrooms,
        int bathrooms,
        decimal areaSqm,
        decimal? latitude,
        decimal? longitude,
        Features features,
        PropertyStatus? newStatus = null)
    {
        if (IsDeleted)
            throw new InvalidPropertyOperationException("Cannot update a deleted property.");
        if (Status == PropertyStatus.Sold)
            throw new InvalidPropertyOperationException("Cannot update a property that has already been sold/rented.");

        if (price <= 0)
            throw new InvalidPropertyOperationException("Property price must be greater than zero.");
        if (string.IsNullOrWhiteSpace(title))
            throw new InvalidPropertyOperationException("Property title is required.");
        if (string.IsNullOrWhiteSpace(city))
            throw new InvalidPropertyOperationException("Property city is required.");
        if (string.IsNullOrWhiteSpace(address))
            throw new InvalidPropertyOperationException("Property address is required.");
        if (areaSqm <= 0)
            throw new InvalidPropertyOperationException("Property area in square meters must be greater than zero.");
        if (bedrooms < 0)
            throw new InvalidPropertyOperationException("Bedrooms cannot be negative.");
        if (bathrooms < 0)
            throw new InvalidPropertyOperationException("Bathrooms cannot be negative.");

        Title = title.Trim();
        Description = description?.Trim() ?? string.Empty;
        Price = price;
        PropertyType = propertyType;
        ListingType = listingType;
        City = city.Trim();
        Address = address.Trim();
        Bedrooms = bedrooms;
        Bathrooms = bathrooms;
        AreaSqm = areaSqm;
        Latitude = latitude;
        Longitude = longitude;
        Features = features;

        if (newStatus.HasValue)
        {
            ToggleStatus(newStatus.Value);
        }

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Owner can toggle status between Available and Pending.
    /// Sold status is terminal and set ONLY via MarkAsSold().
    /// </summary>
    public void ToggleStatus(PropertyStatus newStatus)
    {
        if (Status == PropertyStatus.Sold)
            throw new InvalidPropertyOperationException("Cannot change status of a property that is already Sold.");

        if (newStatus == PropertyStatus.Sold)
            throw new InvalidPropertyOperationException("Property status can only be set to Sold when an offer is accepted.");

        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Terminal domain method called when an offer is accepted.</summary>
    public void MarkAsSold()
    {
        if (IsDeleted)
            throw new InvalidPropertyOperationException("Cannot sell a deleted property.");
        if (Status == PropertyStatus.Sold)
            throw new InvalidPropertyOperationException("Property is already marked as Sold.");

        Status = PropertyStatus.Sold;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        if (IsDeleted) return;

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void EnsureIsAvailable()
    {
        if (IsDeleted || Status != PropertyStatus.Available)
            throw new PropertyUnavailableException($"Property '{PublicId}' is not available for offers. Current status: {Status}");
    }

    // ── Image Management — Invariant: Exactly One Primary ────────────────────────

    public PropertyImage AddImage(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            throw new ArgumentException("Image URL is required.", nameof(imageUrl));

        bool isPrimary = Images.Count == 0;
        var image = PropertyImage.Create(Id, imageUrl, isPrimary);
        Images.Add(image);
        UpdatedAt = DateTime.UtcNow;
        return image;
    }

    public void SetPrimaryImage(Guid imagePublicId)
    {
        var targetImage = Images.FirstOrDefault(i => i.PublicId == imagePublicId)
            ?? throw new NotFoundException($"Image with id '{imagePublicId}' not found on property.");

        foreach (var img in Images)
        {
            if (img.PublicId == imagePublicId)
                img.MarkAsPrimary();
            else
                img.DemoteFromPrimary();
        }

        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveImage(Guid imagePublicId)
    {
        var targetImage = Images.FirstOrDefault(i => i.PublicId == imagePublicId)
            ?? throw new NotFoundException($"Image with id '{imagePublicId}' not found on property.");

        bool wasPrimary = targetImage.IsPrimary;
        Images.Remove(targetImage);

        if (wasPrimary && Images.Count > 0)
        {
            Images.First().MarkAsPrimary();
        }

        UpdatedAt = DateTime.UtcNow;
    }

    // EF Core requires parameterless constructor for hydration.
    private Property() { }
}
