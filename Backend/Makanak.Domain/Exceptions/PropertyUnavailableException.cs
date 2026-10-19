namespace Makanak.Domain.Exceptions;

public sealed class PropertyUnavailableException : DomainException
{
    public PropertyUnavailableException(string message = "The requested property is no longer available.")
        : base(message)
    {
    }
}
