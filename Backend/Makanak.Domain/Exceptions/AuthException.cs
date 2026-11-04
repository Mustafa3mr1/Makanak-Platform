namespace Makanak.Domain.Exceptions;

/// <summary>
/// Thrown when auth-related operations fail (registration, login).
/// Intentionally separate from offer/property exceptions for correct HTTP mapping.
/// </summary>
public sealed class AuthException : DomainException
{
    public AuthException(string message) : base(message) { }
}

