using Makanak.Application.Common.Interfaces;
using Makanak.Domain.Entities;
using Makanak.Domain.Enums;
using Makanak.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Makanak.Infrastructure.Repositories;

public class OfferRepository : GenericRepository<Offer>, IOfferRepository
{
    public OfferRepository(MakanakDbContext context) : base(context) { }

    public async Task<bool> TryClaimPropertyAsync(Guid propertyId, Guid ownerId, PropertyStatus newStatus)
    {
        var now = DateTime.UtcNow;
        var rows = await Context.Properties
            .Where(p => p.Id == propertyId && p.OwnerId == ownerId && p.Status == PropertyStatus.Available)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Status, newStatus)
                .SetProperty(p => p.UpdatedAtUtc, now));
        return rows == 1;
    }
}
