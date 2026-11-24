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

    /// <summary>Initial offer price submitted by seeker in Round 1.</summary>
    public decimal InitialPrice { get; private set; }

    /// <summary>Counter-offer price submitted by owner in Round 2 (nullable).</summary>
    public decimal? CounterPrice { get; private set; }

    public OfferStatus Status { get; private set; } = OfferStatus.Pending;

    /// <summary>Current round number (Round 1: initial offer, Round 2: counter-offer).</summary>
    public int RoundNumber { get; private set; } = 1;

    /// <summary>Concurrency token for optimistic locking (NFR-03).</summary>
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    // Navigation Properties
    public virtual Property Property { get; private set; } = null!;
    public virtual ApplicationUser Seeker { get; private set; } = null!;

    // ── Factory ──────────────────────────────────────────────────────────────────

    public static Offer Create(long propertyId, long seekerId, decimal initialPrice)
    {
        if (initialPrice <= 0)
            throw new InvalidOfferOperationException("Initial offer price must be greater than zero.");

        return new Offer
        {
            PropertyId = propertyId,
            SeekerId = seekerId,
            InitialPrice = initialPrice,
            CounterPrice = null,
            RoundNumber = 1,
            Status = OfferStatus.Pending
        };
    }

    // ── Rich Domain Methods — Invariant Enforcement ───────────────────────────────

    /// <summary>Submits a counter-offer, enforcing the strict 2-round negotiation limit.</summary>
    public void Counter(decimal counterPrice)
    {
        if (RoundNumber >= MaxNegotiationRounds)
            throw new NegotiationLimitExceededException(
                $"Negotiation round limit of {MaxNegotiationRounds} has been reached for offer {PublicId}.");

        if (Status != OfferStatus.Pending)
            throw new InvalidOfferOperationException(
                $"Cannot counter an offer with status '{Status}'. Only Pending offers can be countered.");

        if (counterPrice <= 0)
            throw new InvalidOfferOperationException("Counter offer price must be greater than zero.");

        RoundNumber++;
        CounterPrice = counterPrice;
        Status = OfferStatus.Countered;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Marks the offer as Accepted.</summary>
    public void Accept()
    {
        if (Status != OfferStatus.Pending && Status != OfferStatus.Countered)
            throw new InvalidOfferOperationException(
                $"Cannot accept offer with status '{Status}'. Only Pending or Countered offers can be accepted.");

        Status = OfferStatus.Accepted;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Marks the offer as Rejected.</summary>
    public void Reject()
    {
        if (Status == OfferStatus.Accepted)
            throw new InvalidOfferOperationException("Cannot reject an offer that has already been accepted.");

        Status = OfferStatus.Rejected;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Pure domain function: calculates the list of allowed actions ("accept" | "counter" | "reject")
    /// for a given caller role and property state.
    /// This is the single source of truth for UI actions.
    /// </summary>
    public IReadOnlyList<string> GetAllowedActions(string callerRole, PropertyStatus propertyStatus, bool isPropertyDeleted = false)
    {
        // If property is Sold or soft-deleted, no actions on any offer
        if (isPropertyDeleted || propertyStatus == PropertyStatus.Sold)
            return Array.Empty<string>();

        // If offer is terminal (Accepted or Rejected), no actions
        if (Status == OfferStatus.Accepted || Status == OfferStatus.Rejected)
            return Array.Empty<string>();

        bool isOwner = string.Equals(callerRole, "Owner", StringComparison.OrdinalIgnoreCase);
        bool isSeeker = string.Equals(callerRole, "Seeker", StringComparison.OrdinalIgnoreCase);

        // Pending (Round 1)
        if (Status == OfferStatus.Pending)
        {
            if (isOwner)
            {
                return new[] { "accept", "counter", "reject" };
            }
            if (isSeeker)
            {
                return new[] { "reject" }; // Withdraw
            }
        }

        // Countered (Round 2, FINAL)
        if (Status == OfferStatus.Countered)
        {
            if (isSeeker)
            {
                return new[] { "accept", "reject" };
            }
            if (isOwner)
            {
                return Array.Empty<string>(); // Owner has no actions on counter
            }
        }

        return Array.Empty<string>();
    }

    // EF Core parameterless constructor for hydration.
    private Offer() { }
}
