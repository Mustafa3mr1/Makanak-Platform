using Makanak.Domain.Enums;

namespace Makanak.Domain.Entities;

public class Offer
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public Guid SeekerId { get; set; }

    public decimal CurrentPrice { get; set; }
    public OfferStatus Status { get; set; }

    public int RoundCount { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Property Property { get; set; } = null!;
    public ICollection<NegotiationHistory> History { get; set; } = new List<NegotiationHistory>();
}
