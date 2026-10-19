using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using Makanak.Application.Common.Interfaces;
using Makanak.Domain.Entities;
using Makanak.Domain.Enums;
using Makanak.Infrastructure.Data;

namespace Makanak.Infrastructure.Repositories;

public class PropertyRepository : IPropertyRepository
{
    private readonly ApplicationDbContext _context;
    private readonly GeometryFactory _geometryFactory;

    public PropertyRepository(ApplicationDbContext context)
    {
        _context = context;
        _geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
    }

    public async Task<Property?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Properties
            .Include(p => p.Owner)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Property>> GetNearbyPropertiesAsync(
        double latitude,
        double longitude,
        double radiusInKm,
        CancellationToken cancellationToken = default)
    {
        // Coordinates in WGS 84 (Longitude, Latitude)
        var searchCenter = _geometryFactory.CreatePoint(new Coordinate(longitude, latitude));
        double radiusInMeters = radiusInKm * 1000.0;

        return await _context.Properties
            .AsNoTracking()
            .Where(p => p.Status == PropertyStatus.Available)
            .Where(p => p.Location.Distance(searchCenter) <= radiusInMeters)
            .OrderBy(p => p.Location.Distance(searchCenter))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Property property, CancellationToken cancellationToken = default)
    {
        await _context.Properties.AddAsync(property, cancellationToken);
    }

    public void Update(Property property)
    {
        _context.Properties.Update(property);
    }
}
