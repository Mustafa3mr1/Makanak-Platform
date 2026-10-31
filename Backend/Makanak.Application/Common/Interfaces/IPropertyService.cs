using Makanak.Application.Common.Models;
using Makanak.Application.DTOs.Property;

namespace Makanak.Application.Common.Interfaces;

public interface IPropertyService
{
    Task<PropertyDetailDto> CreateAsync(Guid ownerId, CreatePropertyRequest request);
    Task<PagedResult<PropertyListItemDto>> SearchAsync(PropertySearchQuery query);
    Task<PropertyDetailDto> GetByIdAsync(Guid id, Guid? currentUserId, bool isAdmin);
    Task<PropertyDetailDto> UpdateAsync(Guid id, Guid userId, UpdatePropertyRequest request);
    Task<PropertyDetailDto> ChangeStatusAsync(Guid id, Guid userId, bool isAdmin, ChangePropertyStatusRequest request);
    Task DeleteAsync(Guid id, Guid userId, bool isAdmin);

    Task<PropertyImageDto> AddImageAsync(Guid propertyId, Guid userId, Stream content, long declaredLength);
    Task SetPrimaryImageAsync(Guid propertyId, Guid imageId, Guid userId);
    Task DeleteImageAsync(Guid propertyId, Guid imageId, Guid userId);
}
