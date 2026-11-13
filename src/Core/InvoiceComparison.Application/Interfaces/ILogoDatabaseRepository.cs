namespace InvoiceComparison.Application.Interfaces;

public interface ILogoDatabaseRepository
{
    Task<HashSet<string>> GetExistingEttnsAsync(
        string companyId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);
}
