using Makanak.Domain.Enums;
using Makanak.Domain.Exceptions;

namespace Makanak.Domain.Entities;

/// <summary>
/// Negotiation Engine Model: manages offer state transitions and strictly guards the 2-round limit.
/// </summary>
public class Offer
{
    public const int MaxNegotiationRounds = 2;

    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PropertyId { get; set; }

    public Guid SeekerId { get; set; }

    public decimal CurrentPrice { get; set; }

    public OfferStatus Status { get; set; } = OfferStatus.Pending;

    /// <summary>
    /// Tracks counter-offer iterations between Owner and Seeker (Max: 2).
    /// </summary>
    public int RoundCount { get; set; } = 0;

    /// <summary>
    /// Concurrency token for optimistic locking during concurrent offer updates.
    /// </summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    // Navigation Properties
    public virtual Property Property { get; set; } = null!;

    public virtual ApplicationUser Seeker { get; set; } = null!;

    // Rich Domain Methods - Invariant Enforcement
    /// <summary>
    /// Submits a counter-offer, enforcing the strict 2-round negotiation limit.
    /// </summary>
    public void Counter(decimal counterPrice)
    {
        if (Status != OfferStatus.Pending && Status != OfferStatus.Countered)
        {
            throw new InvalidOfferOperationException($"Cannot counter an offer with status '{Status}'. Only Pending or Countered offers can be countered.");
        }

        if (counterPrice <= 0)
        {
            throw new InvalidOfferOperationException("Counter offer price must be greater than zero.");
        }

        if (RoundCount >= MaxNegotiationRounds)
        {
            throw new NegotiationLimitExceededException($"Negotiation round limit of {MaxNegotiationRounds} has been reached for offer {Id}.");
        }

        RoundCount++;
        CurrentPrice = counterPrice;
        Status = OfferStatus.Countered;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the offer as Accepted.
    /// </summary>
    public void Accept()
    {
        if (Status != OfferStatus.Pending && Status != OfferStatus.Countered)
        {
            throw new InvalidOfferOperationException($"Cannot accept offer with status '{Status}'. Only Pending or Countered offers can be accepted.");
        }

        Status = OfferStatus.Accepted;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the offer as Rejected.
    /// </summary>
    public void Reject()
    {
        if (Status == OfferStatus.Accepted)
        {
            throw new InvalidOfferOperationException("Cannot reject an offer that has already been accepted.");
        }

        Status = OfferStatus.Rejected;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
