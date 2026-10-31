using FluentValidation;
using FluentValidation.Results;
using Makanak.Application.Common.Exceptions;
using Makanak.Application.Common.Interfaces;
using Makanak.Application.Common.Models;
using Makanak.Application.DTOs.Property;
using Makanak.Domain.Entities;
using Makanak.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DomainProperty = Makanak.Domain.Entities.Property;

namespace Makanak.Application.Services;

public sealed class PropertyService : IPropertyService
{
    private readonly IUnitOfWork _uow;
    private readonly IUserAccountGateway _users;
    private readonly IImageStorageService _storage;
    private readonly IValidator<CreatePropertyRequest> _createValidator;
    private readonly IValidator<UpdatePropertyRequest> _updateValidator;
    private readonly IValidator<ChangePropertyStatusRequest> _statusValidator;
    private readonly IValidator<PropertySearchQuery> _searchValidator;
    private readonly ILogger<PropertyService> _logger;

    public PropertyService(
        IUnitOfWork uow, IUserAccountGateway users, IImageStorageService storage,
        IValidator<CreatePropertyRequest> createValidator,
        IValidator<UpdatePropertyRequest> updateValidator,
        IValidator<ChangePropertyStatusRequest> statusValidator,
        IValidator<PropertySearchQuery> searchValidator,
        ILogger<PropertyService> logger)
    {
        _uow = uow;
        _users = users;
        _storage = storage;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _statusValidator = statusValidator;
        _searchValidator = searchValidator;
        _logger = logger;
    }

    // ------------------------------------------------------------------ CRUD

    public async Task<PropertyDetailDto> CreateAsync(Guid ownerId, CreatePropertyRequest request)
    {
        await _createValidator.ValidateAndThrowAsync(request);

        var now = DateTime.UtcNow;
        var property = new DomainProperty
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId, // always from the authenticated principal, never from the body
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Price = request.Price,
            City = request.City.Trim(),
            Address = request.Address.Trim(),
            PropertyType = request.PropertyType,
            ListingType = request.ListingType,
            Status = PropertyStatus.Available,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await _uow.Properties.AddAsync(property);
        await _uow.SaveChangesAsync();

        return await LoadDetailAsync(property.Id);
    }

    public async Task<PagedResult<PropertyListItemDto>> SearchAsync(PropertySearchQuery query)
    {
        await _searchValidator.ValidateAndThrowAsync(query);

        IQueryable<DomainProperty> q = _uow.Properties.QueryNoTracking()
            .Where(p => p.Status == PropertyStatus.Available); // public browsing: available listings only

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var k = query.Keyword.Trim();
            q = q.Where(p => p.Title.Contains(k) || p.Description.Contains(k) || p.City.Contains(k) || p.Address.Contains(k));
        }
        if (!string.IsNullOrWhiteSpace(query.City))
        {
            var city = query.City.Trim();
            q = q.Where(p => p.City == city);
        }
        if (query.MinPrice.HasValue) q = q.Where(p => p.Price >= query.MinPrice.Value);
        if (query.MaxPrice.HasValue) q = q.Where(p => p.Price <= query.MaxPrice.Value);
        if (query.PropertyType.HasValue) q = q.Where(p => p.PropertyType == query.PropertyType.Value);
        if (query.ListingType.HasValue) q = q.Where(p => p.ListingType == query.ListingType.Value);

        var total = await q.CountAsync();

        // Deterministic ordering: the Id tiebreaker guarantees stable pages.
        IOrderedQueryable<DomainProperty> ordered = (query.SortBy, query.SortDirection) switch
        {
            (PropertySortBy.Price, SortDirection.Asc) => q.OrderBy(p => p.Price).ThenBy(p => p.Id),
            (PropertySortBy.Price, _) => q.OrderByDescending(p => p.Price).ThenBy(p => p.Id),
            (PropertySortBy.Title, SortDirection.Asc) => q.OrderBy(p => p.Title).ThenBy(p => p.Id),
            (PropertySortBy.Title, _) => q.OrderByDescending(p => p.Title).ThenBy(p => p.Id),
            (_, SortDirection.Asc) => q.OrderBy(p => p.CreatedAtUtc).ThenBy(p => p.Id),
            _ => q.OrderByDescending(p => p.CreatedAtUtc).ThenBy(p => p.Id)
        };

        var items = await ordered
            .ToPage(query)
            .Select(p => new PropertyListItemDto(
                p.Id, p.Title, p.Price, p.City, p.PropertyType, p.ListingType, p.Status,
                p.Images.Where(i => i.IsPrimary).Select(i => i.ImageUrl).FirstOrDefault(),
                p.CreatedAtUtc))
            .ToListAsync();

        return PagedResult<PropertyListItemDto>.Create(items, total, query.PageNumber, query.PageSize);
    }

    public async Task<PropertyDetailDto> GetByIdAsync(Guid id, Guid? currentUserId, bool isAdmin)
    {
        var property = await _uow.Properties.QueryNoTracking()
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException("Property not found.");

        // Inactive (soft-deleted/deactivated) listings are visible only to their owner and admins.
        if (property.Status == PropertyStatus.Inactive && !isAdmin && property.OwnerId != currentUserId)
            throw new NotFoundException("Property not found.");

        return await MapDetailAsync(property);
    }

    public async Task<PropertyDetailDto> UpdateAsync(Guid id, Guid userId, UpdatePropertyRequest request)
    {
        await _updateValidator.ValidateAndThrowAsync(request);

        var property = await _uow.Properties.GetByIdAsync(id) ?? throw new NotFoundException("Property not found.");
        EnsureOwner(property, userId);

        if (property.Status is PropertyStatus.Sold or PropertyStatus.Rented)
            throw new BusinessRuleException($"A {property.Status} property can no longer be edited.");

        if (request.RowVersion is not null)
            _uow.SetOriginalRowVersion(property, Convert.FromBase64String(request.RowVersion));

        property.Title = request.Title.Trim();
        property.Description = request.Description.Trim();
        property.Price = request.Price;
        property.City = request.City.Trim();
        property.Address = request.Address.Trim();
        property.PropertyType = request.PropertyType;
        property.ListingType = request.ListingType;
        // OwnerId and Status are intentionally NOT updatable here.

        await _uow.SaveChangesAsync();
        return await LoadDetailAsync(id);
    }

    public async Task<PropertyDetailDto> ChangeStatusAsync(Guid id, Guid userId, bool isAdmin, ChangePropertyStatusRequest request)
    {
        await _statusValidator.ValidateAndThrowAsync(request);

        var property = await _uow.Properties.GetByIdAsync(id) ?? throw new NotFoundException("Property not found.");
        if (!isAdmin) EnsureOwner(property, userId);

        if (property.Status is PropertyStatus.Sold or PropertyStatus.Rented)
            throw new BusinessRuleException($"A {property.Status} property status can no longer be changed.");

        // Sold / Rented can only be reached by accepting an offer.
        if (request.Status is not (PropertyStatus.Available or PropertyStatus.Inactive))
            throw new BusinessRuleException("Status can only be changed to Available or Inactive. Sold/Rented are set by accepting an offer.");

        if (property.Status != request.Status)
        {
            if (request.RowVersion is not null)
                _uow.SetOriginalRowVersion(property, Convert.FromBase64String(request.RowVersion));

            property.Status = request.Status;
            await _uow.SaveChangesAsync();
        }

        return await LoadDetailAsync(id);
    }

    public async Task DeleteAsync(Guid id, Guid userId, bool isAdmin)
    {
        var property = await _uow.Properties.GetByIdAsync(id) ?? throw new NotFoundException("Property not found.");
        if (!isAdmin) EnsureOwner(property, userId);

        if (property.Status is PropertyStatus.Sold or PropertyStatus.Rented)
            throw new BusinessRuleException($"A {property.Status} property is part of a completed deal and cannot be removed.");

        if (property.Status != PropertyStatus.Inactive)
        {
            property.Status = PropertyStatus.Inactive;
            await _uow.SaveChangesAsync();
        }
    }

    // ---------------------------------------------------------------- Images

    public async Task<PropertyImageDto> AddImageAsync(Guid propertyId, Guid userId, Stream content, long declaredLength)
    {
        var property = await _uow.Properties.QueryNoTracking()
            .Where(p => p.Id == propertyId)
            .Select(p => new { p.Id, p.OwnerId })
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException("Property not found.");

        if (property.OwnerId != userId)
            throw new ForbiddenException("You can only manage images of your own properties.");

        if (declaredLength <= 0)
            throw Invalid("file", "The uploaded file is empty.");
        if (declaredLength > ImageRules.MaxFileSizeBytes)
            throw new PayloadTooLargeException($"Image must not exceed {ImageRules.MaxFileSizeBytes / (1024 * 1024)} MB.");

        // Read at most limit+1 bytes: never trust the declared length alone.
        var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await content.ReadAsync(chunk.AsMemory(0, chunk.Length))) > 0)
        {
            buffer.Write(chunk, 0, read);
            if (buffer.Length > ImageRules.MaxFileSizeBytes)
                throw new PayloadTooLargeException($"Image must not exceed {ImageRules.MaxFileSizeBytes / (1024 * 1024)} MB.");
        }
        if (buffer.Length == 0) throw Invalid("file", "The uploaded file is empty.");

        var header = buffer.GetBuffer().AsSpan(0, (int)Math.Min(ImageSignature.HeaderLength, buffer.Length));
        var extension = ImageSignature.DetectExtension(header)
            ?? throw Invalid("file", "Only JPEG, PNG and WebP images are allowed (file content was not recognised as one of these).");

        var imageCount = await _uow.PropertyImages.QueryNoTracking().CountAsync(i => i.PropertyId == propertyId);
        if (imageCount >= ImageRules.MaxImagesPerProperty)
            throw Invalid("file", $"A property can have at most {ImageRules.MaxImagesPerProperty} images.");

        buffer.Position = 0;
        var url = await _storage.SaveAsync(propertyId, buffer, extension);

        try
        {
            var hasPrimary = await _uow.PropertyImages.QueryNoTracking().AnyAsync(i => i.PropertyId == propertyId && i.IsPrimary);
            var image = new PropertyImage { Id = Guid.NewGuid(), PropertyId = propertyId, ImageUrl = url, IsPrimary = !hasPrimary };
            await _uow.PropertyImages.AddAsync(image);

            try
            {
                await _uow.SaveChangesAsync();
            }
            catch (ConflictException) when (image.IsPrimary)
            {
                // A concurrent upload became primary first (filtered unique index fired). Keep this one as non-primary.
                image.IsPrimary = false;
                await _uow.SaveChangesAsync();
            }

            return new PropertyImageDto(image.Id, image.ImageUrl, image.IsPrimary);
        }
        catch
        {
            // Compensation: the DB write failed, so remove the file we just stored.
            await SafeDeleteFileAsync(url);
            throw;
        }
    }

    public async Task SetPrimaryImageAsync(Guid propertyId, Guid imageId, Guid userId)
    {
        await EnsureOwnerOfPropertyAsync(propertyId, userId);

        var exists = await _uow.PropertyImages.QueryNoTracking().AnyAsync(i => i.Id == imageId && i.PropertyId == propertyId);
        if (!exists) throw new NotFoundException("Image not found.");

        // Clear + set inside one transaction so the filtered unique index (one primary per property) is never violated
        // and the property never ends up with zero or two primaries.
        await _uow.ExecuteInTransactionAsync(async () =>
        {
            await _uow.Properties.ClearPrimaryImagesAsync(propertyId);
            var ok = await _uow.Properties.SetPrimaryImageAsync(propertyId, imageId);
            if (!ok) throw new NotFoundException("Image not found.");
            return true;
        });
    }

    public async Task DeleteImageAsync(Guid propertyId, Guid imageId, Guid userId)
    {
        await EnsureOwnerOfPropertyAsync(propertyId, userId);

        var url = await _uow.PropertyImages.QueryNoTracking()
            .Where(i => i.Id == imageId && i.PropertyId == propertyId)
            .Select(i => i.ImageUrl)
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException("Image not found.");

        await _uow.ExecuteInTransactionAsync(async () =>
        {
            var image = await _uow.PropertyImages.Query()
                .FirstOrDefaultAsync(i => i.Id == imageId && i.PropertyId == propertyId)
                ?? throw new NotFoundException("Image not found.");

            var wasPrimary = image.IsPrimary;
            _uow.PropertyImages.Remove(image);
            await _uow.SaveChangesAsync();

            if (wasPrimary)
            {
                // Promote another image so the property keeps a primary whenever it has any images.
                var nextId = await _uow.PropertyImages.QueryNoTracking()
                    .Where(i => i.PropertyId == propertyId)
                    .OrderBy(i => i.Id)
                    .Select(i => (Guid?)i.Id)
                    .FirstOrDefaultAsync();
                if (nextId.HasValue)
                    await _uow.Properties.SetPrimaryImageAsync(propertyId, nextId.Value);
            }
            return true;
        });

        // File removal happens after the DB commit; a failure only leaves an orphaned file (logged), never a dangling DB row.
        await SafeDeleteFileAsync(url);
    }

    // --------------------------------------------------------------- Helpers

    private static void EnsureOwner(DomainProperty property, Guid userId)
    {
        if (property.OwnerId != userId)
            throw new ForbiddenException("You can only modify your own properties.");
    }

    private async Task EnsureOwnerOfPropertyAsync(Guid propertyId, Guid userId)
    {
        var ownerId = await _uow.Properties.QueryNoTracking()
            .Where(p => p.Id == propertyId)
            .Select(p => (Guid?)p.OwnerId)
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException("Property not found.");

        if (ownerId != userId)
            throw new ForbiddenException("You can only manage images of your own properties.");
    }

    private async Task SafeDeleteFileAsync(string url)
    {
        try { await _storage.DeleteAsync(url); }
        catch (Exception ex) { _logger.LogWarning(ex, "Could not delete stored image file {Url}; it may be orphaned.", url); }
    }

    private static ValidationException Invalid(string field, string message)
        => new(new[] { new ValidationFailure(field, message) });

    private async Task<PropertyDetailDto> LoadDetailAsync(Guid id)
    {
        var property = await _uow.Properties.QueryNoTracking()
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException("Property not found.");
        return await MapDetailAsync(property);
    }

    private async Task<PropertyDetailDto> MapDetailAsync(DomainProperty p)
    {
        var names = await _users.GetNamesAsync(new[] { p.OwnerId });
        var images = p.Images
            .OrderByDescending(i => i.IsPrimary).ThenBy(i => i.Id)
            .Select(i => new PropertyImageDto(i.Id, i.ImageUrl, i.IsPrimary))
            .ToList();

        return new PropertyDetailDto(
            p.Id, p.Title, p.Description, p.Price, p.City, p.Address, p.PropertyType, p.ListingType, p.Status,
            new PropertyOwnerDto(p.OwnerId, names.TryGetValue(p.OwnerId, out var n) ? n : "Unknown"),
            images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl,
            images,
            Convert.ToBase64String(p.RowVersion ?? Array.Empty<byte>()),
            p.CreatedAtUtc, p.UpdatedAtUtc);
    }
}
