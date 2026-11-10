using InvoiceComparison.Domain.Enums;

namespace InvoiceComparison.Domain.Entities;

public sealed record InvoiceDetail
{
    public required string Ettn { get; init; }
    public required string InvoiceNumber { get; init; }
    public required string SenderTaxNumber { get; init; }
    public required string SenderTitle { get; init; }
    public required string ReceiverTaxNumber { get; init; }
    public required string ReceiverTitle { get; init; }
    public required DateTime IssueDate { get; init; }
    public DateTime? EnvelopeDate { get; init; }
    public string? EnvelopeIdentifier { get; init; }
    public required string ProfileId { get; init; }
    public required string InvoiceType { get; init; }
    public required string CurrencyCode { get; init; }
    public decimal ExchangeRate { get; init; } = 1m;
    public decimal LineExtensionAmount { get; init; }
    public decimal TaxExclusiveAmount { get; init; }
    public decimal TaxInclusiveAmount { get; init; }
    public decimal PayableAmount { get; init; }
    public decimal AllowanceTotalAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public string? Note { get; init; }
    public InvoiceIntegrationStatus IntegrationStatus { get; init; }
    public IReadOnlyList<InvoiceLineItem> Lines { get; init; } = [];
}

public sealed record InvoiceLineItem
{
    public required int LineNumber { get; init; }
    public string? ItemCode { get; init; }
    public required string ItemName { get; init; }
    public required decimal Quantity { get; init; }
    public required string UnitCode { get; init; }
    public required decimal UnitPrice { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal VatRate { get; init; }
    public decimal VatAmount { get; init; }
    public required decimal LineTotalAmount { get; init; }
}

