using InvoiceComparison.Application.DTOs;

namespace InvoiceComparison.Application.Interfaces;

public interface ILogoDatabaseRepository
{
    Task<IReadOnlyList<InvoiceIdentityDto>> GetExistingInvoicesAsync(
        string companyId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);
}
