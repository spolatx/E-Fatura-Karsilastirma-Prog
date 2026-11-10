using InvoiceComparison.Domain.Enums;

namespace InvoiceComparison.Domain.Entities;

public sealed record InvoiceSummary
{
    public required string Ettn { get; init; }
    public required string InvoiceNumber { get; init; }
    public required string SenderTaxNumber { get; init; }
    public required string SenderTitle { get; init; }
    public required DateTime IssueDate { get; init; }
    public required decimal PayableAmount { get; init; }
    public decimal TaxExclusiveAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public string CurrencyCode { get; init; } = "TRY";
    public string ProfileId { get; init; } = string.Empty;
    public string InvoiceType { get; init; } = string.Empty;
    public InvoiceIntegrationStatus IntegrationStatus { get; init; } = InvoiceIntegrationStatus.Unknown;
}

