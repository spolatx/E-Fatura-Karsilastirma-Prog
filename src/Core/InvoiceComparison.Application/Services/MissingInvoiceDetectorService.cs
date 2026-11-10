using InvoiceComparison.Application.DTOs;
using InvoiceComparison.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace InvoiceComparison.Application.Services;

public sealed class MissingInvoiceDetectorService : IMissingInvoiceDetectorService
{
    private readonly IELogoIntegrationService _eLogoIntegrationService;
    private readonly ILogoDatabaseRepository _logoDatabaseRepository;
    private readonly ILogger<MissingInvoiceDetectorService> _logger;

    public MissingInvoiceDetectorService(
        IELogoIntegrationService eLogoIntegrationService,
        ILogoDatabaseRepository logoDatabaseRepository,
        ILogger<MissingInvoiceDetectorService> logger)
    {
        _eLogoIntegrationService = eLogoIntegrationService ?? throw new ArgumentNullException(nameof(eLogoIntegrationService));
        _logoDatabaseRepository = logoDatabaseRepository ?? throw new ArgumentNullException(nameof(logoDatabaseRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<MissingInvoicesResponse> DetectMissingInvoicesAsync(
        MissingInvoicesRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateDateRange(request.StartDate, request.EndDate);

        var normalizedStartDate = request.StartDate.Date;
        var normalizedEndDate = request.EndDate.Date.AddDays(1).AddTicks(-1);

        _logger.LogInformation(
            "Starting missing invoice detection between {StartDate:yyyy-MM-dd} and {EndDate:yyyy-MM-dd}.",
            normalizedStartDate,
            normalizedEndDate);

        // Execute integrator API call and local ERP SQL query concurrently
        var eLogoInvoicesTask = _eLogoIntegrationService.GetInvoicesAsync(
            normalizedStartDate,
            normalizedEndDate,
            cancellationToken);

        var logoEttnsTask = _logoDatabaseRepository.GetExistingEttnsAsync(
            normalizedStartDate,
            normalizedEndDate,
            cancellationToken);

        await Task.WhenAll(eLogoInvoicesTask, logoEttnsTask).ConfigureAwait(false);

        var eLogoInvoices = await eLogoInvoicesTask.ConfigureAwait(false);
        var rawLogoEttns = await logoEttnsTask.ConfigureAwait(false);

        // Ensure case-insensitive HashSet comparison
        var logoEttns = new HashSet<string>(
            rawLogoEttns.Where(ettn => !string.IsNullOrWhiteSpace(ettn)).Select(ettn => ettn.Trim()),
            StringComparer.OrdinalIgnoreCase);

        var missingInvoices = eLogoInvoices
            .Where(x => !string.IsNullOrWhiteSpace(x.Ettn) && !logoEttns.Contains(x.Ettn.Trim()))
            .OrderByDescending(x => x.IssueDate)
            .ThenBy(x => x.InvoiceNumber, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var matchedCount = eLogoInvoices.Count - missingInvoices.Count;

        _logger.LogInformation(
            "Comparison completed. eLogo Count: {TotalCount}, Matched in Logo ERP: {MatchedCount}, Missing in Logo ERP: {MissingCount}.",
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
        string ettn,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ettn);
        var normalizedEttn = ettn.Trim();

        _logger.LogInformation("Requesting invoice PDF from eLogo for ETTN: {Ettn}", normalizedEttn);
        return await _eLogoIntegrationService.GetInvoicePdfAsync(normalizedEttn, cancellationToken).ConfigureAwait(false);
    }

    public async Task<MarkInvoiceUnreadResponse> MarkInvoiceAsUnreadAsync(
        string ettn,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ettn);
        var normalizedEttn = ettn.Trim();

        _logger.LogInformation("Marking invoice as unread (returning to Inbox) in eLogo for ETTN: {Ettn}", normalizedEttn);
        var isSuccess = await _eLogoIntegrationService.MarkAsUnreadAsync(normalizedEttn, cancellationToken).ConfigureAwait(false);

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
