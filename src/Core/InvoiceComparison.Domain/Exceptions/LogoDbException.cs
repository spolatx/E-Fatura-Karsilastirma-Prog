namespace InvoiceComparison.Domain.Exceptions;

public sealed class LogoDbException : DomainException
{
    public int? SqlErrorNumber { get; }
    public string? DatabaseOperation { get; }

    public LogoDbException(
        string message,
        string? databaseOperation = null,
        int? sqlErrorNumber = null,
        Exception? innerException = null)
        : base(message, "LOGO_DB_ERROR", innerException)
    {
        DatabaseOperation = databaseOperation;
        SqlErrorNumber = sqlErrorNumber;
    }
}

