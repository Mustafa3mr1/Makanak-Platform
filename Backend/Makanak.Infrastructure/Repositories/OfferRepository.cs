using Microsoft.EntityFrameworkCore;
using Makanak.Application.Common.Interfaces;
using Makanak.Domain.Entities;
using Makanak.Domain.Enums;
using Makanak.Infrastructure.Data;

namespace Makanak.Infrastructure.Repositories;

public class OfferRepository : IOfferRepository
{
    private readonly ApplicationDbContext _context;

    public OfferRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Offer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Offers
            .Include(o => o.Seeker)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<Offer?> GetByIdWithPropertyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Offers
            .Include(o => o.Property)
            .Include(o => o.Seeker)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Offer>> GetPendingOffersByPropertyIdAsync(
        Guid propertyId,
        Guid excludeOfferId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Offers
            .Where(o => o.PropertyId == propertyId && o.Id != excludeOfferId && (o.Status == OfferStatus.Pending || o.Status == OfferStatus.Countered))
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
