using Makanak.Domain.Entities;

namespace Makanak.Application.Common.Interfaces;

/// <summary>
/// Repository for appending immutable <see cref="NegotiationHistory"/> audit records.
/// Write-only: history rows are never updated or deleted.
/// </summary>
public interface INegotiationHistoryRepository
{
    void Add(NegotiationHistory history);
}

