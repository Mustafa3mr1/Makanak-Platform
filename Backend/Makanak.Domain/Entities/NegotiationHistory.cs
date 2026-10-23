using Makanak.Domain.Enums;

namespace Makanak.Domain.Entities;

/// <summary>
/// Immutable audit record capturing every negotiation action on an <see cref="Offer"/> (FR-09).
/// Each counter, accept, or reject operation appends one entry to this log.
/// </summary>
public class NegotiationHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key to the <see cref="Offer"/> this history entry belongs to.
    /// </summary>
    public Guid OfferId { get; set; }

    /// <summary>
    /// The <see cref="ApplicationUser"/> Id of the actor who performed the action
    /// (Owner or Seeker).
    /// </summary>
    public Guid ActionByUserId { get; set; }

    /// <summary>
    /// The type of negotiation action taken (e.g. Submitted, Countered, Accepted, Rejected).
    /// </summary>
    public NegotiationActionType ActionType { get; set; }

    /// <summary>
    /// The price proposed or agreed upon at the time of this action.
    /// </summary>
    public decimal ProposedPrice { get; set; }

    /// <summary>
    /// UTC timestamp of when the action occurred.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual Offer Offer { get; set; } = null!;

    public virtual ApplicationUser ActionByUser { get; set; } = null!;
}
