using Microsoft.EntityFrameworkCore;
using Makanak.Application.Common.Interfaces;
using Makanak.Application.Common.Models;
using Makanak.Application.DTOs.Properties;
using Makanak.Domain.Entities;
using Makanak.Domain.Enums;
using Makanak.Domain.Exceptions;

namespace Makanak.Infrastructure.Services;

/// <summary>
/// Property management service.
/// Uses Unit of Work and Repository abstractions; zero DbContext leakage into Application layer.
/// IDOR safe: not-owner lookups on non-available properties return 404.
/// </summary>
public sealed class PropertyService : IPropertyService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;

    public PropertyService(
        IUnitOfWork unitOfWork,
        IFileStorageService fileStorageService)
    {
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
    }

    public async Task<PropertyDetailsDto> CreatePropertyAsync(
        CreatePropertyRequestDto request,
        long ownerInternalId,
        CancellationToken cancellationToken = default)
    {
        var features = ParseFeatures(request.Features);

        var property = Property.Create(
            ownerId: ownerInternalId,
            title: request.Title,
            description: request.Description,
            price: request.Price,
            propertyType: request.PropertyType,
            listingType: request.ListingType,
            city: request.City,
            address: request.Address,
            bedrooms: request.Bedrooms,
            bathrooms: request.Bathrooms,
            areaSqm: request.AreaSqm,
            latitude: request.Latitude,
            longitude: request.Longitude,
            features: features);

        await _unitOfWork.Properties.AddAsync(property, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var owner = await _unitOfWork.Users.GetByIdAsync(ownerInternalId, cancellationToken);

        return MapToDetailsDto(property, owner?.FullName ?? string.Empty, owner?.PublicId ?? Guid.Empty, callerIsOwner: true);
    }

    public async Task<PropertyDetailsDto?> GetPropertyByPublicIdAsync(
        Guid publicId,
        long? callerInternalId = null,
        CancellationToken cancellationToken = default)
    {
        var property = await _unitOfWork.Properties.GetByPublicIdWithImagesAndOwnerAsync(publicId, cancellationToken);
        if (property == null || property.IsDeleted)
            return null;

        bool callerIsOwner = callerInternalId.HasValue && property.OwnerId == callerInternalId.Value;

        // Only Available properties are public. If non-available and caller is not owner -> 404 (IDOR safe)
        if (property.Status != PropertyStatus.Available && !callerIsOwner)
            return null;

        return MapToDetailsDto(
            property,
            property.Owner?.FullName ?? string.Empty,
            property.Owner?.PublicId ?? Guid.Empty,
            callerIsOwner);
    }

    public async Task<PagedResult<PropertyListItemDto>> GetPropertiesAsync(
        PropertyFilterQueryDto filter,
        CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Properties.GetPagedListAsync(filter, cancellationToken);
    }

    public async Task<PagedResult<PropertyListItemDto>> GetOwnerPropertiesAsync(
        long ownerInternalId,
        PropertyStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Properties.GetOwnerPropertiesAsync(ownerInternalId, status, page, pageSize, cancellationToken);
    }

    public async Task<PropertyDetailsDto> UpdatePropertyAsync(
        Guid publicId,
        UpdatePropertyRequestDto request,
        long ownerInternalId,
        CancellationToken cancellationToken = default)
    {
        var property = await _unitOfWork.Properties.GetByPublicIdWithImagesAndOwnerAsync(publicId, cancellationToken);
        if (property == null || property.OwnerId != ownerInternalId)
        {
            throw new NotFoundException($"Property '{publicId}' not found.");
        }

        var currentToken = Convert.ToBase64String(property.RowVersion);
        if (!string.Equals(currentToken, request.RowVersion, StringComparison.Ordinal))
        {
            throw new DbUpdateConcurrencyException("Concurrency conflict: the property has been modified by another process.");
        }

        var features = ParseFeatures(request.Features);

        property.Update(
            title: request.Title,
            description: request.Description,
            price: request.Price,
            propertyType: request.PropertyType,
            listingType: request.ListingType,
            city: request.City,
            address: request.Address,
            bedrooms: request.Bedrooms,
            bathrooms: request.Bathrooms,
            areaSqm: request.AreaSqm,
            latitude: request.Latitude,
            longitude: request.Longitude,
            features: features,
            newStatus: request.Status);

        _unitOfWork.Properties.Update(property);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDetailsDto(
            property,
            property.Owner?.FullName ?? string.Empty,
            property.Owner?.PublicId ?? Guid.Empty,
            callerIsOwner: true);
    }

    public async Task SoftDeletePropertyAsync(
        Guid publicId,
        long ownerInternalId,
        CancellationToken cancellationToken = default)
    {
        var property = await _unitOfWork.Properties.GetByPublicIdAsync(publicId, cancellationToken);
        if (property == null || property.OwnerId != ownerInternalId)
        {
            throw new NotFoundException($"Property '{publicId}' not found.");
        }

        property.SoftDelete();
        _unitOfWork.Properties.Update(property);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PropertyImageItemDto>> AddImagesAsync(
        Guid propertyPublicId,
        IReadOnlyList<(Stream Stream, string Extension)> files,
        long ownerInternalId,
        CancellationToken cancellationToken = default)
    {
        var property = await _unitOfWork.Properties.GetByPublicIdWithImagesAsync(propertyPublicId, cancellationToken);
        if (property == null || property.OwnerId != ownerInternalId)
        {
            throw new NotFoundException($"Property '{propertyPublicId}' not found.");
        }

        var folder = $"uploads/properties/{property.PublicId}";
        var addedImages = new List<PropertyImage>();

        foreach (var file in files)
        {
            var relativeUrl = await _fileStorageService.SaveFileAsync(file.Stream, file.Extension, folder, cancellationToken);
            var image = property.AddImage(relativeUrl);
            addedImages.Add(image);
        }

        _unitOfWork.Properties.Update(property);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return property.Images
            .Select(i => new PropertyImageItemDto(i.PublicId, i.ImageUrl, i.IsPrimary))
            .ToList();
    }

    public async Task<PropertyImageItemDto> SetPrimaryImageAsync(
        Guid propertyPublicId,
        Guid imagePublicId,
        long ownerInternalId,
        CancellationToken cancellationToken = default)
    {
        var property = await _unitOfWork.Properties.GetByPublicIdWithImagesAsync(propertyPublicId, cancellationToken);
        if (property == null || property.OwnerId != ownerInternalId)
        {
            throw new NotFoundException($"Property '{propertyPublicId}' not found.");
        }

        property.SetPrimaryImage(imagePublicId);
        _unitOfWork.Properties.Update(property);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var primary = property.Images.First(i => i.PublicId == imagePublicId);
        return new PropertyImageItemDto(primary.PublicId, primary.ImageUrl, primary.IsPrimary);
    }

    public async Task DeleteImageAsync(
        Guid propertyPublicId,
        Guid imagePublicId,
        long ownerInternalId,
        CancellationToken cancellationToken = default)
    {
        var property = await _unitOfWork.Properties.GetByPublicIdWithImagesAsync(propertyPublicId, cancellationToken);
        if (property == null || property.OwnerId != ownerInternalId)
        {
            throw new NotFoundException($"Property '{propertyPublicId}' not found.");
        }

        var targetImage = property.Images.FirstOrDefault(i => i.PublicId == imagePublicId)
            ?? throw new NotFoundException($"Image '{imagePublicId}' not found on property.");

        var imageUrl = targetImage.ImageUrl;

        property.RemoveImage(imagePublicId);
        _unitOfWork.Properties.Update(property);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _fileStorageService.DeleteFileAsync(imageUrl, cancellationToken);
    }

    private static PropertyDetailsDto MapToDetailsDto(
        Property property,
        string ownerFullName,
        Guid ownerPublicId,
        bool callerIsOwner)
    {
        var images = property.Images
            .Select(i => new PropertyImageItemDto(i.PublicId, i.ImageUrl, i.IsPrimary))
            .ToList();

        var primaryImage = images.FirstOrDefault(i => i.IsPrimary)?.Url
            ?? images.FirstOrDefault()?.Url;

        var featuresList = GetFeatureStrings(property.Features);

        return new PropertyDetailsDto(
            PublicId: property.PublicId,
            Title: property.Title,
            Price: property.Price,
            ListingType: property.ListingType,
            PropertyType: property.PropertyType,
            Status: property.Status,
            City: property.City,
            Bedrooms: property.Bedrooms,
            Bathrooms: property.Bathrooms,
            AreaSqm: property.AreaSqm,
            PrimaryImageUrl: primaryImage,
            CreatedAt: property.CreatedAt,
            Description: property.Description,
            Address: property.Address,
            Latitude: property.Latitude,
            Longitude: property.Longitude,
            Features: featuresList,
            Images: images,
            Owner: new PropertyOwnerDto(ownerPublicId, ownerFullName),
            RowVersion: callerIsOwner ? Convert.ToBase64String(property.RowVersion) : null
        );
    }

    private static Features ParseFeatures(IReadOnlyList<string>? features)
    {
        if (features == null || features.Count == 0)
            return Features.None;

        var result = Features.None;
        foreach (var f in features)
        {
            if (Enum.TryParse<Features>(f, ignoreCase: true, out var parsed))
            {
                result |= parsed;
            }
        }
        return result;
    }

    private static IReadOnlyList<string> GetFeatureStrings(Features features)
    {
        var list = new List<string>();
        foreach (Features f in Enum.GetValues(typeof(Features)))
        {
            if (f != Features.None && features.HasFlag(f))
            {
                list.Add(f.ToString());
            }
        }
        return list;
    }
}
