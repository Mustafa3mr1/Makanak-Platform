using Makanak.Domain.Enums;
using Makanak.Domain.Exceptions;

namespace Makanak.Domain.Entities;

/// <summary>
/// Negotiation Engine aggregate: manages offer state transitions and guards the 2-round limit.
/// Hybrid ID: <see cref="BaseEntity.Id"/> (internal FK join key) + <see cref="BaseEntity.PublicId"/> (API surface).
/// All FKs to related aggregates use <c>long</c> internal IDs for clustered index performance.
/// </summary>
public class Offer : BaseEntity
{
    public const int MaxNegotiationRounds = 2;

    // ── Internal FKs (long) ──────────────────────────────────────────────────────
    public long PropertyId { get; private set; }
    public long SeekerId { get; private set; }

    public decimal CurrentPrice { get; private set; }

    public OfferStatus Status { get; private set; } = OfferStatus.Pending;

    /// <summary>Tracks counter-offer iterations (Max: 2).</summary>
    public int RoundCount { get; private set; } = 0;

    /// <summary>Concurrency token for optimistic locking (NFR-03).</summary>
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; private set; }

    // Navigation Properties
    public virtual Property Property { get; private set; } = null!;
    public virtual ApplicationUser Seeker { get; private set; } = null!;

    // ── Factory ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Domain factory — only valid way to open a new negotiation offer.
    /// Validates price and links to aggregate root IDs.
    /// </summary>
    public static Offer Create(long propertyId, long seekerId, decimal initialPrice)
    {
        if (initialPrice <= 0)
            throw new InvalidOfferOperationException("Initial offer price must be greater than zero.");

        return new Offer
        {
            PropertyId = propertyId,
            SeekerId = seekerId,
            CurrentPrice = initialPrice
        };
    }

    // ── Rich Domain Methods — Invariant Enforcement ───────────────────────────────

    /// <summary>Submits a counter-offer, enforcing the strict 2-round negotiation limit.</summary>
    public void Counter(decimal counterPrice)
    {
        if (Status != OfferStatus.Pending && Status != OfferStatus.Countered)
            throw new InvalidOfferOperationException(
                $"Cannot counter an offer with status '{Status}'. Only Pending or Countered offers can be countered.");

        if (counterPrice <= 0)
            throw new InvalidOfferOperationException("Counter offer price must be greater than zero.");

        if (RoundCount >= MaxNegotiationRounds)
            throw new NegotiationLimitExceededException(
                $"Negotiation round limit of {MaxNegotiationRounds} has been reached for offer {PublicId}.");

        RoundCount++;
        CurrentPrice = counterPrice;
        Status = OfferStatus.Countered;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Marks the offer as Accepted.</summary>
    public void Accept()
    {
        if (Status != OfferStatus.Pending && Status != OfferStatus.Countered)
            throw new InvalidOfferOperationException(
                $"Cannot accept offer with status '{Status}'. Only Pending or Countered offers can be accepted.");

        Status = OfferStatus.Accepted;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Marks the offer as Rejected.</summary>
    public void Reject()
    {
        if (Status == OfferStatus.Accepted)
            throw new InvalidOfferOperationException("Cannot reject an offer that has already been accepted.");

        Status = OfferStatus.Rejected;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    // EF Core parameterless constructor for hydration.
    private Offer() { }
}
