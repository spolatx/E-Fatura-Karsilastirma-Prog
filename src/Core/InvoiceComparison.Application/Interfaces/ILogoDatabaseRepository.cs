using InvoiceComparison.Application.DTOs;

namespace InvoiceComparison.Application.Interfaces;

public interface ILogoDatabaseRepository
{
    Task<IReadOnlyList<InvoiceIdentityDto>> GetPurchaseInvoicesAsync(
        string companyId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InvoiceIdentityDto>> GetSalesInvoicesAsync(
        string companyId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);
}
