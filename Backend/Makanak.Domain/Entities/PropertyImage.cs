namespace Makanak.Domain.Entities;

/// <summary>
/// Image associated with a property (FR-04).
/// Uses BaseEntity Hybrid ID pattern.
/// </summary>
public class PropertyImage : BaseEntity
{
    // Internal FK
    public long PropertyId { get; private set; }

    public string ImageUrl { get; private set; } = string.Empty;

    public bool IsPrimary { get; private set; }

    // Navigation
    public virtual Property Property { get; private set; } = null!;

    public static PropertyImage Create(long propertyId, string imageUrl, bool isPrimary = false)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            throw new ArgumentException("Image URL cannot be empty.", nameof(imageUrl));

        return new PropertyImage
        {
            PropertyId = propertyId,
            ImageUrl = imageUrl,
            IsPrimary = isPrimary
        };
    }

    private PropertyImage() { }
}
