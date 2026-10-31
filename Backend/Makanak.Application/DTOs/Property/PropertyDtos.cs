using Makanak.Application.Common.Models;
using Makanak.Domain.Enums;

namespace Makanak.Application.DTOs.Property;

public interface IPropertyFields
{
    string Title { get; }
    string Description { get; }
    decimal Price { get; }
    string City { get; }
    string Address { get; }
    PropertyType PropertyType { get; }
    ListingType ListingType { get; }
}

public sealed record CreatePropertyRequest(
    string Title, string Description, decimal Price, string City, string Address,
    PropertyType PropertyType, ListingType ListingType) : IPropertyFields;

public sealed record UpdatePropertyRequest(
    string Title, string Description, decimal Price, string City, string Address,
    PropertyType PropertyType, ListingType ListingType, string? RowVersion) : IPropertyFields;

public sealed record ChangePropertyStatusRequest(PropertyStatus Status, string? RowVersion);

public class PropertySearchQuery : PagedQuery
{
    public string? Keyword { get; set; }
    public string? City { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public PropertyType? PropertyType { get; set; }
    public ListingType? ListingType { get; set; }
    public PropertySortBy SortBy { get; set; } = PropertySortBy.CreatedAt;
    public SortDirection SortDirection { get; set; } = SortDirection.Desc;
}

public enum PropertySortBy { CreatedAt = 1, Price = 2, Title = 3 }
public enum SortDirection { Asc = 1, Desc = 2 }

public sealed record PropertyListItemDto(
    Guid Id, string Title, decimal Price, string City, PropertyType PropertyType, ListingType ListingType,
    PropertyStatus Status, string? PrimaryImageUrl, DateTime CreatedAtUtc);

public sealed record PropertyImageDto(Guid Id, string ImageUrl, bool IsPrimary);

public sealed record PropertyOwnerDto(Guid Id, string FullName);

public sealed record PropertyDetailDto(
    Guid Id, string Title, string Description, decimal Price, string City, string Address,
    PropertyType PropertyType, ListingType ListingType, PropertyStatus Status,
    PropertyOwnerDto Owner, string? PrimaryImageUrl, IReadOnlyList<PropertyImageDto> Images,
    string RowVersion, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
