namespace Makanak.Domain.Exceptions;

/// <summary>
/// Thrown when negotiation round limit is exceeded.
/// File name matches class name exactly (fixes previous mismatch).
/// </summary>
public sealed class NegotiationLimitExceededException : DomainException
{
    public NegotiationLimitExceededException(string message) : base(message) { }
}
