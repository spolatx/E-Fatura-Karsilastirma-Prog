namespace InvoiceComparison.Application.Interfaces;

public interface ILogoDatabaseRepository
{
    Task<HashSet<string>> GetExistingEttnsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);
}
