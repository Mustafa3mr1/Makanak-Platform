namespace Makanak.Domain.Exceptions;

public sealed class ForbiddenActionException : DomainException
{
    public ForbiddenActionException(string message) : base(message, "forbidden_action") { }
}
