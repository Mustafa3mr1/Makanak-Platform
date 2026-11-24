using Makanak.Domain.Enums;

namespace Makanak.Domain.Entities;

/// <summary>
/// Immutable audit record capturing every negotiation action on an <see cref="Offer"/> (FR-09).
/// History rows are append-only and never updated or deleted (DeleteBehavior.Restrict).
/// </summary>
public class NegotiationHistory
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>FK to the Offer (internal long Id) this history entry belongs to.</summary>
    public long OfferId { get; private set; }

    /// <summary>FK to the ApplicationUser (internal long Id) who performed the action.</summary>
    public long ActionByUserId { get; private set; }

    public NegotiationActionType ActionType { get; private set; }

    public decimal ProposedPrice { get; private set; }

    public int RoundNumber { get; private set; } = 1;

    public DateTime Timestamp { get; private set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual Offer Offer { get; private set; } = null!;
    public virtual ApplicationUser ActionByUser { get; private set; } = null!;

    // ── Factory ──────────────────────────────────────────────────────────────────

    public static NegotiationHistory Record(
        long offerId,
        long actionByUserId,
        NegotiationActionType actionType,
        decimal proposedPrice,
        int roundNumber,
        DateTime? timestamp = null)
    {
        return new NegotiationHistory
        {
            OfferId = offerId,
            ActionByUserId = actionByUserId,
            ActionType = actionType,
            ProposedPrice = proposedPrice,
            RoundNumber = roundNumber,
            Timestamp = timestamp ?? DateTime.UtcNow
        };
    }

    private NegotiationHistory() { }
}
