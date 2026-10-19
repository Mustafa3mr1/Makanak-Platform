namespace Makanak.Domain.Exceptions;

public sealed class InvalidOfferOperationException : DomainException
{
    public InvalidOfferOperationException(string message) : base(message)
    {
    }
}
