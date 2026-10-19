using NetTopologySuite;
using NetTopologySuite.Geometries;
using Makanak.Application.Common.Interfaces;
using Makanak.Application.DTOs.Properties;
using Makanak.Domain.Entities;
using Makanak.Domain.Enums;

namespace Makanak.Infrastructure.Services;

public class PropertyService : IPropertyService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly GeometryFactory _geometryFactory;

    public PropertyService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
    }

    public async Task<PropertyDto> CreatePropertyAsync(
        CreatePropertyRequestDto request,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        // NetTopologySuite uses (Longitude, Latitude) for coordinate order
        var location = _geometryFactory.CreatePoint(new Coordinate(request.Longitude, request.Latitude));

        var property = new Property
        {
            OwnerId = ownerId,
            Title = request.Title,
            Description = request.Description,
            Price = request.Price,
            Status = PropertyStatus.Available,
            Location = location
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

    public async Task<IReadOnlyList<PropertyDto>> GetNearbyPropertiesAsync(
        double latitude,
        double longitude,
        double radiusInKm,
        CancellationToken cancellationToken = default)
    {
        var properties = await _unitOfWork.Properties.GetNearbyPropertiesAsync(latitude, longitude, radiusInKm, cancellationToken);
        return properties.Select(MapToDto).ToList();
    }

    private static PropertyDto MapToDto(Property p) => new(
        p.Id,
        p.OwnerId,
        p.Title,
        p.Description,
        p.Price,
        p.Status,
        p.Location.Y, // Latitude
        p.Location.X, // Longitude
        p.CreatedAtUtc
    );
}
