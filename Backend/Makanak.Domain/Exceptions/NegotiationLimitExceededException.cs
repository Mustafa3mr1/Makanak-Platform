namespace Makanak.Domain.Exceptions;

public sealed class NegotiationLimitExceededException : DomainException
{
    public NegotiationLimitExceededException(string message = "The negotiation round limit of 2 rounds has been exceeded.")
        : base(message)
    {
    }
}
