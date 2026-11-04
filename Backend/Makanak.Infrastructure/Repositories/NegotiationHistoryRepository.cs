using Makanak.Application.Common.Interfaces;
using Makanak.Domain.Entities;
using Makanak.Infrastructure.Data;

namespace Makanak.Infrastructure.Repositories;

public sealed class NegotiationHistoryRepository : INegotiationHistoryRepository
{
    private readonly ApplicationDbContext _context;

    public NegotiationHistoryRepository(ApplicationDbContext context) => _context = context;

    /// <summary>
    /// Appends an immutable history record to the DbContext change tracker.
    /// SaveChanges is called by the UnitOfWork — not here — ensuring atomicity.
    /// </summary>
    public void Add(NegotiationHistory history)
    {
        _context.NegotiationHistories.Add(history);
    }
}

