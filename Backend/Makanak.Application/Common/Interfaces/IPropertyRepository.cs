using Makanak.Domain.Entities;

namespace Makanak.Application.Common.Interfaces;

public interface IPropertyRepository
{
    Task<Property?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Property>> GetNearbyPropertiesAsync(double latitude, double longitude, double radiusInKm, CancellationToken cancellationToken = default);
    Task AddAsync(Property property, CancellationToken cancellationToken = default);
    void Update(Property property);
}
