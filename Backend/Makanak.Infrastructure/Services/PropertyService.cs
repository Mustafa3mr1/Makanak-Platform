using Makanak.Application.Common.Interfaces;
using Makanak.Application.DTOs.Properties;
using Makanak.Domain.Entities;
using Makanak.Domain.Enums;

namespace Makanak.Infrastructure.Services;

public class PropertyService : IPropertyService
{
    private readonly IUnitOfWork _unitOfWork;

    public PropertyService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PropertyDto> CreatePropertyAsync(
        CreatePropertyRequestDto request,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var property = new Property
        {
            OwnerId = ownerId,
            Title = request.Title,
            Description = request.Description,
            Price = request.Price,
            Status = PropertyStatus.Available,
            City = request.City,
            Address = request.Address
        };

        await _unitOfWork.Properties.AddAsync(property, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(property);
    }

    public async Task<PropertyDto?> GetPropertyByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var property = await _unitOfWork.Properties.GetByIdAsync(id, cancellationToken);
        return property == null ? null : MapToDto(property);
    }

    public async Task<IReadOnlyList<PropertyDto>> GetAllPropertiesAsync(CancellationToken cancellationToken = default)
    {
        var properties = await _unitOfWork.Properties.GetAllAsync(cancellationToken);
        return properties.Select(MapToDto).ToList();
    }

    private static PropertyDto MapToDto(Property p) => new(
        p.Id,
        p.OwnerId,
        p.Title,
        p.Description,
        p.Price,
        p.Status,
        p.City,
        p.Address,
        p.CreatedAtUtc
    );
}

