using InvoiceComparison.Domain.Entities;

namespace InvoiceComparison.Application.DTOs;

public sealed record MissingInvoicesResponse
{
    public required DateTime StartDate { get; init; }
    public required DateTime EndDate { get; init; }
    public required int TotalIntegratorInvoicesCount { get; init; }
    public required int TotalErpMatchedCount { get; init; }
    public required int MissingInvoicesCount { get; init; }
    public required IReadOnlyList<InvoiceSummary> MissingInvoices { get; init; }
    public IReadOnlyList<InvoiceSummary> MatchedInvoices { get; init; } = [];
}


