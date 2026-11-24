using Makanak.Domain.Entities;

namespace Makanak.Application.Common.Interfaces;

/// <summary>
/// Repository for appending and querying immutable <see cref="NegotiationHistory"/> audit records.
/// History rows are append-only and never updated or deleted.
/// </summary>
public interface INegotiationHistoryRepository
{
    void Add(NegotiationHistory history);
    Task<IReadOnlyList<NegotiationHistory>> GetHistoryByOfferIdAsync(long offerId, CancellationToken cancellationToken = default);
}
