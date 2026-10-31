using Makanak.Domain.Entities;

namespace Makanak.Application.Common.Interfaces;

public interface IPropertyRepository : IRepository<Property>
{
    Task ClearPrimaryImagesAsync(Guid propertyId);
    Task<bool> SetPrimaryImageAsync(Guid propertyId, Guid imageId);
}
