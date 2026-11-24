namespace Makanak.Domain.Exceptions;

public class AuthException : DomainException
{
    public AuthException(string message) : base(message, "authentication_failed") { }
}
