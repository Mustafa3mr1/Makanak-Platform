namespace Makanak.Domain.Exceptions;

public sealed class NegotiationLimitExceededException : DomainException
{
    public NegotiationLimitExceededException(string message) : base(message, "negotiation_limit_exceeded") { }
}
