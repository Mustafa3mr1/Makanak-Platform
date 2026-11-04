using Makanak.Domain.Entities;

namespace Makanak.Application.Common.Interfaces;

/// <summary>
/// Repository for <see cref="Property"/> aggregates.
/// All lookup methods by external identifier use <c>Guid publicId</c>.
/// Internal <c>long Id</c> is never exposed across this boundary.
/// </summary>
public interface IPropertyRepository
{
    Task<Property?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default);
    Task<Property?> GetByPublicIdWithImagesAsync(Guid publicId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Property>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Property property, CancellationToken cancellationToken = default);
    void Update(Property property);
}
