using Microsoft.EntityFrameworkCore;
using Makanak.Application.Common.Interfaces;
using Makanak.Domain.Entities;
using Makanak.Infrastructure.Data;

namespace Makanak.Infrastructure.Repositories;

public sealed class NegotiationHistoryRepository : INegotiationHistoryRepository
{
    private readonly ApplicationDbContext _context;

    public NegotiationHistoryRepository(ApplicationDbContext context) => _context = context;

    public void Add(NegotiationHistory history)
    {
        _context.NegotiationHistories.Add(history);
    }

    public async Task<IReadOnlyList<NegotiationHistory>> GetHistoryByOfferIdAsync(
        long offerId,
        CancellationToken cancellationToken = default)
    {
        return await _context.NegotiationHistories
            .AsNoTracking()
            .Include(nh => nh.ActionByUser)
            .Where(nh => nh.OfferId == offerId)
            .OrderBy(nh => nh.Timestamp)
            .ToListAsync(cancellationToken);
    }
}
