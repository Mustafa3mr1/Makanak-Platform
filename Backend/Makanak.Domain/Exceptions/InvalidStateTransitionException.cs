namespace Makanak.Domain.Exceptions;

public sealed class InvalidStateTransitionException : DomainException
{
    public InvalidStateTransitionException(string message) : base(message, "invalid_state_transition") { }
}
