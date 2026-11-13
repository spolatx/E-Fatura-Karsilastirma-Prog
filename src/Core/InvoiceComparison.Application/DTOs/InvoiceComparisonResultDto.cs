using InvoiceComparison.Domain.Entities;

namespace InvoiceComparison.Application.DTOs;

public class InvoiceComparisonResultDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalIntegratorInvoicesCount { get; set; }
    public int TotalErpMatchedCount { get; set; }
    public int MissingInvoicesCount { get; set; }
    public List<InvoiceSummary> MissingInvoices { get; set; } = new();
    public List<InvoiceSummary> MatchedInvoices { get; set; } = new();
}

