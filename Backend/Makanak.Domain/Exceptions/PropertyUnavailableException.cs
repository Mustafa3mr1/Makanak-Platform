namespace Makanak.Domain.Exceptions;

public sealed class PropertyUnavailableException : DomainException
{
    public PropertyUnavailableException(string message) : base(message, "property_not_available") { }
}
