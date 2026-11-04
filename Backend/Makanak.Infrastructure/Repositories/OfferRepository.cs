using Microsoft.EntityFrameworkCore;
using Makanak.Application.Common.Interfaces;
using Makanak.Domain.Entities;
using Makanak.Domain.Enums;
using Makanak.Infrastructure.Data;

namespace Makanak.Infrastructure.Repositories;

public sealed class OfferRepository : IOfferRepository
{
    private readonly ApplicationDbContext _context;

    public OfferRepository(ApplicationDbContext context) => _context = context;

    public async Task<Offer?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default)
    {
        return await _context.Offers
            .FirstOrDefaultAsync(o => o.PublicId == publicId, cancellationToken);
    }

    public async Task<Offer?> GetByPublicIdWithPropertyAsync(Guid publicId, CancellationToken cancellationToken = default)
    {
        return await _context.Offers
            .Include(o => o.Property)
            .FirstOrDefaultAsync(o => o.PublicId == publicId, cancellationToken);
    }

    /// <summary>
    /// Gets competing offers by internal long FK — called only within the service after resolving the offer internally.
    /// Uses internal long Ids for the most efficient clustered-index range scan.
    /// </summary>
    public async Task<IReadOnlyList<Offer>> GetPendingOffersByPropertyIdAsync(
        long propertyId,
        long excludeOfferId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Offers
            .Where(o => o.PropertyId == propertyId
                     && o.Id != excludeOfferId
                     && (o.Status == OfferStatus.Pending || o.Status == OfferStatus.Countered))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Offer offer, CancellationToken cancellationToken = default)
    {
        await _context.Offers.AddAsync(offer, cancellationToken);
    }

    public void Update(Offer offer)
    {
        _context.Offers.Update(offer);
    }
}
