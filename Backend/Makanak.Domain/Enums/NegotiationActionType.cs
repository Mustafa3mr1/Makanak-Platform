namespace Makanak.Domain.Enums;

/// <summary>
/// Represents the type of action recorded in a <see cref="Entities.NegotiationHistory"/> entry.
/// </summary>
public enum NegotiationActionType
{
    /// <summary>Initial offer submitted by the Seeker.</summary>
    Submitted = 1,

    /// <summary>Counter-offer issued (by Owner or Seeker within the 2-round limit).</summary>
    Countered = 2,

    /// <summary>Offer was accepted by the Owner.</summary>
    Accepted = 3,

    /// <summary>Offer was rejected by the Owner.</summary>
    Rejected = 4
}
