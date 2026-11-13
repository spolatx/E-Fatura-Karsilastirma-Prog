using InvoiceComparison.Application.DTOs;
using InvoiceComparison.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace InvoiceComparison.Application.Services;

public sealed class MissingInvoiceDetectorService : IMissingInvoiceDetectorService
{
    private readonly IELogoIntegrationService _eLogoIntegrationService;
    private readonly ILogoDatabaseRepository _logoDatabaseRepository;
    private readonly ICompanyProvider _companyProvider;
    private readonly ILogger<MissingInvoiceDetectorService> _logger;

    public MissingInvoiceDetectorService(
        IELogoIntegrationService eLogoIntegrationService,
        ILogoDatabaseRepository logoDatabaseRepository,
        ICompanyProvider companyProvider,
        ILogger<MissingInvoiceDetectorService> logger)
    {
        _eLogoIntegrationService = eLogoIntegrationService ?? throw new ArgumentNullException(nameof(eLogoIntegrationService));
        _logoDatabaseRepository = logoDatabaseRepository ?? throw new ArgumentNullException(nameof(logoDatabaseRepository));
        _companyProvider = companyProvider ?? throw new ArgumentNullException(nameof(companyProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IReadOnlyList<CompanyDto> GetCompanies()
    {
        return _companyProvider.GetCompanies();
    }

    public async Task<MissingInvoicesResponse> DetectMissingInvoicesAsync(
        MissingInvoicesRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.CompanyId);
        ValidateDateRange(request.StartDate, request.EndDate);

        var normalizedStartDate = request.StartDate.Date;
        var normalizedEndDate = request.EndDate.Date.AddDays(1).AddTicks(-1);

        _logger.LogInformation(
            "Starting missing invoice detection for Company '{CompanyId}' between {StartDate:yyyy-MM-dd} and {EndDate:yyyy-MM-dd}.",
            request.CompanyId,
            normalizedStartDate,
            normalizedEndDate);

        // Execute integrator API call and local ERP SQL query concurrently with companyId
        var eLogoInvoicesTask = _eLogoIntegrationService.GetInvoicesAsync(
            request.CompanyId,
            normalizedStartDate,
            normalizedEndDate,
            cancellationToken);

        var logoEttnsTask = _logoDatabaseRepository.GetExistingEttnsAsync(
            request.CompanyId,
            normalizedStartDate,
            normalizedEndDate,
            cancellationToken);

        await Task.WhenAll(eLogoInvoicesTask, logoEttnsTask).ConfigureAwait(false);

        var eLogoInvoices = await eLogoInvoicesTask.ConfigureAwait(false);
        var rawLogoEttns = await logoEttnsTask.ConfigureAwait(false);

        // Logo ERP'deki mevcut ETTN'leri temizleyerek HashSet'e at (Performans ve Case-Insensitive arama için)
        var logoEttnSet = new HashSet<string>(
            rawLogoEttns.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim().Trim('{', '}')),
            StringComparer.OrdinalIgnoreCase);

        _logger.LogInformation(
            "Company '{CompanyId}' comparison input: eLogo invoices count = {ELogoCount}, Logo ERP unique ETTNs count = {LogoCount}.",
            request.CompanyId,
            eLogoInvoices.Count,
            logoEttnSet.Count);

        var missingInvoices = eLogoInvoices
            .Where(inv => !string.IsNullOrWhiteSpace(inv.Ettn) && !logoEttnSet.Contains(inv.Ettn.Trim().Trim('{', '}')))
            .OrderByDescending(x => x.IssueDate)
            .ThenBy(x => x.InvoiceNumber, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var matchedCount = eLogoInvoices.Count - missingInvoices.Count;

        _logger.LogInformation(
            "Comparison completed for Company '{CompanyId}'. eLogo Count: {TotalCount}, Matched in Logo ERP: {MatchedCount}, Missing in Logo ERP: {MissingCount}.",
            request.CompanyId,
            eLogoInvoices.Count,
            matchedCount,
            missingInvoices.Count);

        return new MissingInvoicesResponse
        {
            StartDate = normalizedStartDate,
            EndDate = normalizedEndDate,
            TotalIntegratorInvoicesCount = eLogoInvoices.Count,
            TotalErpMatchedCount = matchedCount,
            MissingInvoicesCount = missingInvoices.Count,
            MissingInvoices = missingInvoices
        };
    }

    public async Task<byte[]> GetInvoicePdfAsync(
        string companyId,
        string ettn,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(companyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(ettn);
        var normalizedEttn = ettn.Trim();

        _logger.LogInformation("Requesting invoice PDF from eLogo for Company '{CompanyId}', ETTN: {Ettn}", companyId, normalizedEttn);
        return await _eLogoIntegrationService.GetInvoicePdfAsync(companyId, normalizedEttn, cancellationToken).ConfigureAwait(false);
    }

    public async Task<MarkInvoiceUnreadResponse> MarkInvoiceAsUnreadAsync(
        string companyId,
        string ettn,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(companyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(ettn);
        var normalizedEttn = ettn.Trim();

        _logger.LogInformation("Marking invoice as unread (returning to Inbox) in eLogo for Company '{CompanyId}', ETTN: {Ettn}", companyId, normalizedEttn);
        var isSuccess = await _eLogoIntegrationService.MarkAsUnreadAsync(companyId, normalizedEttn, cancellationToken).ConfigureAwait(false);

        return new MarkInvoiceUnreadResponse
        {
            Ettn = normalizedEttn,
            IsSuccess = isSuccess,
            Message = isSuccess
                ? $"Invoice {normalizedEttn} was successfully marked as unread (moved to Inbox)."
                : $"Failed to mark invoice {normalizedEttn} as unread.",
            ProcessedAtUtc = DateTime.UtcNow
        };
    }

    private static void ValidateDateRange(DateTime startDate, DateTime endDate)
    {
        if (endDate.Date < startDate.Date)
        {
            throw new ArgumentException("End date cannot be earlier than start date.", nameof(endDate));
        }

        if ((endDate.Date - startDate.Date).TotalDays > 366)
        {
            throw new ArgumentException("Date range cannot exceed 366 days for a single comparison request.", nameof(endDate));
        }
    }
}
