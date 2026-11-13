namespace InvoiceComparison.Application.DTOs;

public sealed record MissingInvoicesRequest
{
    public required string CompanyId { get; init; }
    public required DateTime StartDate { get; init; }
    public required DateTime EndDate { get; init; }
}
