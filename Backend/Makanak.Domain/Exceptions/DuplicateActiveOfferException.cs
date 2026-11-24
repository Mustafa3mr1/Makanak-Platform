namespace Makanak.Domain.Exceptions;

public sealed class DuplicateActiveOfferException : DomainException
{
    public DuplicateActiveOfferException(string message = "You already have an active offer on this property.")
        : base(message, "duplicate_active_offer") { }
}
