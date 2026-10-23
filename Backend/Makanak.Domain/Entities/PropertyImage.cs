namespace Makanak.Domain.Entities;

/// <summary>
/// Represents an image associated with a property listing (FR-04).
/// A property may have multiple images; exactly one should be designated as primary.
/// </summary>
public class PropertyImage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key to the owning <see cref="Property"/>.
    /// </summary>
    public Guid PropertyId { get; set; }

    /// <summary>
    /// Absolute URL to the image in cloud storage (e.g., Azure Blob / S3).
    /// </summary>
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether this is the primary display image for the listing.
    /// </summary>
    public bool IsPrimary { get; set; } = false;

    // Navigation Properties
    public virtual Property Property { get; set; } = null!;
}
