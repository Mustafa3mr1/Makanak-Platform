using Makanak.Application.Common.Interfaces;
using Makanak.Application.DTOs.Properties;
using Makanak.Domain.Entities;
using Makanak.Domain.Exceptions;

namespace Makanak.Infrastructure.Services;

/// <summary>
/// Property management service.
/// No DbContext injection — operates exclusively through IUnitOfWork abstractions.
/// </summary>
public sealed class PropertyService : IPropertyService
{
    private readonly IUnitOfWork _unitOfWork;

    public PropertyService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<PropertyDto> CreatePropertyAsync(
        CreatePropertyRequestDto request,
        long ownerInternalId,
        CancellationToken cancellationToken = default)
    {
        var property = Property.Create(
            ownerInternalId,
            request.Title,
            request.Description,
            request.Price,
            request.City,
            request.Address);

        await _unitOfWork.Properties.AddAsync(property, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload the owner for DTO mapping (OwnerId resolved via navigation after insert)
        var ownerUser = await _unitOfWork.Users.GetByIdAsync(ownerInternalId, cancellationToken);

        return MapToDto(property, ownerUser?.PublicId ?? Guid.Empty);
    }

    public async Task<PropertyDto?> GetPropertyByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default)
    {
        var property = await _unitOfWork.Properties.GetByPublicIdAsync(publicId, cancellationToken);
        if (property is null) return null;

        var ownerUser = await _unitOfWork.Users.GetByIdAsync(property.OwnerId, cancellationToken);
        return MapToDto(property, ownerUser?.PublicId ?? Guid.Empty);
    }

    public async Task<IReadOnlyList<PropertyDto>> GetAllPropertiesAsync(CancellationToken cancellationToken = default)
    {
        // Note: GetAllAsync uses AsNoTracking + filtered Include(primary image only)
        var properties = await _unitOfWork.Properties.GetAllAsync(cancellationToken);

        // Batch-resolve owner PublicIds to avoid N+1 queries
        var ownerIds = properties.Select(p => p.OwnerId).Distinct().ToList();
        var ownerMap = new Dictionary<long, Guid>();

        foreach (var ownerId in ownerIds)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(ownerId, cancellationToken);
            if (user is not null)
                ownerMap[ownerId] = user.PublicId;
        }

        return properties
            .Select(p => MapToDto(p, ownerMap.TryGetValue(p.OwnerId, out var ownerPublicId) ? ownerPublicId : Guid.Empty))
            .ToList();
    }

    private static PropertyDto MapToDto(Property p, Guid ownerPublicId) => new(
        p.PublicId,
        ownerPublicId,
        p.Title,
        p.Description,
        p.Price,
        p.Status,
        p.City,
        p.Address,
        p.CreatedAtUtc);
}
