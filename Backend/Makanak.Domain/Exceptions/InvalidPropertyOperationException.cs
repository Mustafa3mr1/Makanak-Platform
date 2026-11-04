namespace Makanak.Domain.Exceptions;

/// <summary>Thrown when an invalid operation is attempted on a Property aggregate.</summary>
public sealed class InvalidPropertyOperationException : DomainException
{
    public InvalidPropertyOperationException(string message) : base(message) { }
}

