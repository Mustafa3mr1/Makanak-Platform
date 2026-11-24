namespace Makanak.Domain.Exceptions;

public sealed class OwnPropertyOfferException : DomainException
{
    public OwnPropertyOfferException(string message = "You cannot make an offer on your own property.")
        : base(message, "own_property_offer") { }
}
