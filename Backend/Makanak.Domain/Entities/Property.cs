using Makanak.Domain.Enums;

namespace Makanak.Domain.Entities;

public class Property
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public PropertyStatus Status { get; set; }

    public PropertyType PropertyType { get; set; }
    public ListingType ListingType { get; set; }

    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    public byte[] RowVersion { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<PropertyImage> Images { get; set; } = new List<PropertyImage>();
    public ICollection<Offer> Offers { get; set; } = new List<Offer>();
}
