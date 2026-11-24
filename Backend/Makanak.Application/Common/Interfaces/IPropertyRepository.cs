using Makanak.Application.Common.Models;
using Makanak.Application.DTOs.Properties;
using Makanak.Domain.Entities;
using Makanak.Domain.Enums;

namespace Makanak.Application.Common.Interfaces;

/// <summary>
/// Repository for <see cref="Property"/> aggregates.
/// All lookup methods by external identifier use <c>Guid publicId</c>.
/// Internal <c>long Id</c> is never exposed across this boundary.
/// </summary>
public interface IPropertyRepository
{
    Task<PagedResult<PropertyListItemDto>> GetPagedListAsync(PropertyFilterQueryDto filter, CancellationToken cancellationToken = default);
    Task<PagedResult<PropertyListItemDto>> GetOwnerPropertiesAsync(long ownerId, PropertyStatus? statusFilter, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<(int TotalProperties, int AvailableProperties)> GetOwnerPropertyCountsAsync(long ownerId, CancellationToken cancellationToken = default);
    Task<Property?> GetByPublicIdWithImagesAndOwnerAsync(Guid publicId, CancellationToken cancellationToken = default);
    Task<Property?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default);
    Task<Property?> GetByPublicIdWithImagesAsync(Guid publicId, CancellationToken cancellationToken = default);
    Task AddAsync(Property property, CancellationToken cancellationToken = default);
    void Update(Property property);
}
