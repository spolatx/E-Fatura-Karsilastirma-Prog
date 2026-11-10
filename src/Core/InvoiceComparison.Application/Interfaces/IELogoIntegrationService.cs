using InvoiceComparison.Domain.Entities;

namespace InvoiceComparison.Application.Interfaces;

public interface IELogoIntegrationService
{
    Task<IReadOnlyList<InvoiceSummary>> GetInvoicesAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    Task<byte[]> GetInvoicePdfAsync(
        string ettn,
        CancellationToken cancellationToken = default);

    Task<bool> MarkAsUnreadAsync(
        string ettn,
        CancellationToken cancellationToken = default);
}
