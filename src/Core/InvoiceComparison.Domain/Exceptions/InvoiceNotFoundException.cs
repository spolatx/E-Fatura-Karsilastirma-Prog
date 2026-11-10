namespace InvoiceComparison.Domain.Exceptions;

public sealed class InvoiceNotFoundException : DomainException
{
    public string Ettn { get; }

    public InvoiceNotFoundException(string ettn, Exception? innerException = null)
        : base($"Invoice with ETTN '{ettn}' was not found in the integrator.", "INVOICE_NOT_FOUND", innerException)
    {
        Ettn = ettn;
    }
}

