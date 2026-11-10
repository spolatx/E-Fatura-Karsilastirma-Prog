namespace InvoiceComparison.Domain.Exceptions;

public sealed class ELogoApiException : DomainException
{
    public int? HttpStatusCode { get; }
    public string? IntegratorErrorCode { get; }
    public string? OperationName { get; }

    public ELogoApiException(
        string message,
        string? operationName = null,
        string? integratorErrorCode = null,
        int? httpStatusCode = null,
        Exception? innerException = null)
        : base(message, "ELOGO_API_ERROR", innerException)
    {
        OperationName = operationName;
        IntegratorErrorCode = integratorErrorCode;
        HttpStatusCode = httpStatusCode;
    }
}

