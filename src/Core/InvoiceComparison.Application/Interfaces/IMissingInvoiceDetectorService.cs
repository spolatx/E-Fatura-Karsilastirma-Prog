using InvoiceComparison.Application.DTOs;

namespace InvoiceComparison.Application.Interfaces;

public interface IMissingInvoiceDetectorService
{
    Task<MissingInvoicesResponse> DetectMissingInvoicesAsync(
        MissingInvoicesRequest request,
        CancellationToken cancellationToken = default);

    Task<byte[]> GetInvoicePdfAsync(
        string ettn,
        CancellationToken cancellationToken = default);

    Task<MarkInvoiceUnreadResponse> MarkInvoiceAsUnreadAsync(
        string ettn,
        CancellationToken cancellationToken = default);
}
