using InvoiceComparison.Domain.Entities;

namespace InvoiceComparison.Application.Interfaces;

public interface IELogoIntegrationService
{
    Task<IReadOnlyList<InvoiceSummary>> GetInvoicesAsync(
        string companyId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    Task<byte[]> GetInvoicePdfAsync(
        string companyId,
        string ettn,
        CancellationToken cancellationToken = default);

    Task<bool> MarkAsUnreadAsync(
        string companyId,
        string ettn,
        CancellationToken cancellationToken = default);
}
