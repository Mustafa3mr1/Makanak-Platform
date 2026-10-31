using Makanak.Domain.Entities;
using Makanak.Domain.Enums;

namespace Makanak.Application.Common.Interfaces;

public interface IOfferRepository : IRepository<Offer>
{

    Task<bool> TryClaimPropertyAsync(Guid propertyId, Guid ownerId, PropertyStatus newStatus);
}
