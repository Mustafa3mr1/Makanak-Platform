using Makanak.Domain.Enums;

namespace Makanak.Domain.Entities;

public class NegotiationHistory
{
    public Guid Id { get; set; }
    public Guid OfferId { get; set; }
    public Guid ActionByUserId { get; set; }
    public NegotiationActionType ActionType { get; set; }
    public decimal ProposedPrice { get; set; }
    public DateTime Timestamp { get; set; }

    public Offer Offer { get; set; } = null!;
}
