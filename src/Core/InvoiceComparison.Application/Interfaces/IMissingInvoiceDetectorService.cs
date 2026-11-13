using InvoiceComparison.Application.DTOs;

namespace InvoiceComparison.Application.Interfaces;

public interface IMissingInvoiceDetectorService
{
    IReadOnlyList<CompanyDto> GetCompanies();

    Task<MissingInvoicesResponse> DetectMissingInvoicesAsync(
        MissingInvoicesRequest request,
        CancellationToken cancellationToken = default);

    Task<byte[]> GetInvoicePdfAsync(
        string companyId,
        string ettn,
        CancellationToken cancellationToken = default);

    Task<MarkInvoiceUnreadResponse> MarkInvoiceAsUnreadAsync(
        string companyId,
        string ettn,
        CancellationToken cancellationToken = default);
}
