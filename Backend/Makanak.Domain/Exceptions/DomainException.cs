namespace Makanak.Domain.Exceptions;

public class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string message, string code = "domain_error") : base(message)
    {
        Code = code;
    }

    public DomainException(string message, Exception innerException, string code = "domain_error")
        : base(message, innerException)
    {
        Code = code;
    }
}
