using Makanak.Application.DTOs.Properties;
using Makanak.Application.Validators;
using Makanak.Domain.Entities;
using Makanak.Domain.Enums;
using Makanak.Domain.Exceptions;
using Xunit;

namespace Makanak.Tests;

public class PropertyDomainTests
{
    [Fact]
    public void Property_Create_ShouldInitializeAllFieldsCorrectly()
    {
        var property = Property.Create(
            ownerId: 10,
            title: "Sunny Flat",
            description: "Nice flat in Maadi",
            price: 2500000m,
            propertyType: PropertyType.Apartment,
            listingType: ListingType.Sale,
            city: "Maadi",
            address: "Street 9",
            bedrooms: 2,
            bathrooms: 1,
            areaSqm: 120m,
            latitude: 29.96m,
            longitude: 31.25m,
            features: Features.Parking | Features.Balcony
        );

        Assert.Equal(10, property.OwnerId);
        Assert.Equal("Sunny Flat", property.Title);
        Assert.Equal("Nice flat in Maadi", property.Description);
        Assert.Equal(2500000m, property.Price);
        Assert.Equal(PropertyType.Apartment, property.PropertyType);
        Assert.Equal(ListingType.Sale, property.ListingType);
        Assert.Equal(PropertyStatus.Available, property.Status);
        Assert.Equal("Maadi", property.City);
        Assert.Equal("Street 9", property.Address);
        Assert.Equal(2, property.Bedrooms);
        Assert.Equal(1, property.Bathrooms);
        Assert.Equal(120m, property.AreaSqm);
        Assert.False(property.IsDeleted);
        Assert.True(property.Features.HasFlag(Features.Parking));
        Assert.True(property.Features.HasFlag(Features.Balcony));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Property_Create_WithInvalidPrice_ShouldThrow(decimal invalidPrice)
    {
        Assert.Throws<InvalidPropertyOperationException>(() =>
            Property.Create(1, "Title", "Desc", invalidPrice, PropertyType.Apartment, ListingType.Sale, "City", "Addr", 1, 1, 100m));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Property_Create_WithEmptyTitle_ShouldThrow(string emptyTitle)
    {
        Assert.Throws<InvalidPropertyOperationException>(() =>
            Property.Create(1, emptyTitle, "Desc", 1000m, PropertyType.Apartment, ListingType.Sale, "City", "Addr", 1, 1, 100m));
    }

    [Fact]
    public void Property_ToggleStatus_BetweenAvailableAndPending_ShouldSucceed()
    {
        var property = Property.Create(1, "Title", "Desc", 1000m, PropertyType.Apartment, ListingType.Sale, "City", "Addr", 1, 1, 100m);
        
        property.ToggleStatus(PropertyStatus.Pending);
        Assert.Equal(PropertyStatus.Pending, property.Status);

        property.ToggleStatus(PropertyStatus.Available);
        Assert.Equal(PropertyStatus.Available, property.Status);
    }

    [Fact]
    public void Property_ToggleStatus_ToSold_ShouldThrow()
    {
        var property = Property.Create(1, "Title", "Desc", 1000m, PropertyType.Apartment, ListingType.Sale, "City", "Addr", 1, 1, 100m);
        
        Assert.Throws<InvalidPropertyOperationException>(() =>
            property.ToggleStatus(PropertyStatus.Sold));
    }

    [Fact]
    public void Property_MarkAsSold_ShouldSetStatusToSold()
    {
        var property = Property.Create(1, "Title", "Desc", 1000m, PropertyType.Apartment, ListingType.Sale, "City", "Addr", 1, 1, 100m);
        
        property.MarkAsSold();
        Assert.Equal(PropertyStatus.Sold, property.Status);

        // Terminal: cannot toggle status once sold
        Assert.Throws<InvalidPropertyOperationException>(() =>
            property.ToggleStatus(PropertyStatus.Available));
    }

    [Fact]
    public void Property_SoftDelete_ShouldSetIsDeletedAndDeletedAt()
    {
        var property = Property.Create(1, "Title", "Desc", 1000m, PropertyType.Apartment, ListingType.Sale, "City", "Addr", 1, 1, 100m);
        
        property.SoftDelete();

        Assert.True(property.IsDeleted);
        Assert.NotNull(property.DeletedAt);
    }

    [Fact]
    public void Property_EnsureIsAvailable_ShouldThrowWhenDeletedOrNotAvailable()
    {
        var property = Property.Create(1, "Title", "Desc", 1000m, PropertyType.Apartment, ListingType.Sale, "City", "Addr", 1, 1, 100m);
        property.ToggleStatus(PropertyStatus.Pending);

        Assert.Throws<PropertyUnavailableException>(() => property.EnsureIsAvailable());

        property.ToggleStatus(PropertyStatus.Available);
        property.SoftDelete();

        Assert.Throws<PropertyUnavailableException>(() => property.EnsureIsAvailable());
    }

    [Fact]
    public void PropertyImages_PrimaryImageRule_EnforcedCorrectly()
    {
        var property = Property.Create(1, "Title", "Desc", 1000m, PropertyType.Apartment, ListingType.Sale, "City", "Addr", 1, 1, 100m);

        // 1. First image added becomes primary
        var img1 = property.AddImage("https://example.com/1.jpg");
        Assert.True(img1.IsPrimary);
        Assert.Single(property.Images);

        // 2. Second image added is NOT primary
        var img2 = property.AddImage("https://example.com/2.jpg");
        Assert.False(img2.IsPrimary);
        Assert.True(img1.IsPrimary);
        Assert.Equal(2, property.Images.Count);

        // 3. Setting second image as primary demotes first
        property.SetPrimaryImage(img2.PublicId);
        Assert.True(img2.IsPrimary);
        Assert.False(img1.IsPrimary);

        // 4. Deleting primary promotes next available image
        property.RemoveImage(img2.PublicId);
        Assert.Single(property.Images);
        Assert.True(img1.IsPrimary);
    }

    [Fact]
    public void CreatePropertyValidator_WithValidData_ShouldPass()
    {
        var validator = new CreatePropertyRequestValidator();
        var request = new CreatePropertyRequestDto(
            Title: "Luxury Apartment",
            Description: "Spacious",
            Price: 3500000m,
            ListingType: ListingType.Sale,
            PropertyType: PropertyType.Apartment,
            City: "New Cairo",
            Address: "90th Street",
            Bedrooms: 3,
            Bathrooms: 2,
            AreaSqm: 180m,
            Latitude: 30.01m,
            Longitude: 31.49m,
            Features: new List<string> { "Parking", "Security" }
        );

        var result = validator.Validate(request);
        Assert.True(result.IsValid);
    }
}
