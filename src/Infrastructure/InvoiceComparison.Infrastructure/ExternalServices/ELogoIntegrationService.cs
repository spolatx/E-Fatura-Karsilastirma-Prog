using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using InvoiceComparison.Application.Interfaces;
using InvoiceComparison.Domain.Entities;
using InvoiceComparison.Domain.Enums;
using InvoiceComparison.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace InvoiceComparison.Infrastructure.ExternalServices;

public sealed class ELogoIntegrationService : IELogoIntegrationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<ELogoIntegrationService> _logger;

    public ELogoIntegrationService(
        HttpClient httpClient,
        ILogger<ELogoIntegrationService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<InvoiceSummary>> GetInvoicesAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var requestUri = $"/api/v1/einvoices/inbox?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}";

        try
        {
            using var response = await _httpClient.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            await EnsureSuccessResponseAsync(response, nameof(GetInvoicesAsync), cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var payload = await response.Content
                .ReadFromJsonAsync<ELogoInvoiceListApiResponse>(JsonOptions, cancellationToken)
                .ConfigureAwait(false);

            if (payload?.Invoices is null || payload.Invoices.Count == 0)
            {
                return [];
            }

            return payload.Invoices
                .Select(dto => new InvoiceSummary
                {
                    Ettn = dto.Ettn,
                    InvoiceNumber = dto.InvoiceNumber,
                    SenderTaxNumber = dto.SenderTaxNumber,
                    SenderTitle = dto.SenderTitle,
                    IssueDate = dto.IssueDate,
                    PayableAmount = dto.PayableAmount,
                    TaxExclusiveAmount = dto.TaxExclusiveAmount,
                    TaxAmount = dto.TaxAmount,
                    CurrencyCode = string.IsNullOrWhiteSpace(dto.CurrencyCode) ? "TRY" : dto.CurrencyCode,
                    ProfileId = dto.ProfileId ?? string.Empty,
                    InvoiceType = dto.InvoiceType ?? string.Empty,
                    IntegrationStatus = dto.Status
                })
                .ToList();
        }
        catch (Exception ex) when (ex is not ELogoApiException and not OperationCanceledException)
        {
            _logger.LogError(ex, "Error while fetching incoming e-Invoices from eLogo API for range {StartDate:yyyy-MM-dd} - {EndDate:yyyy-MM-dd}.", startDate, endDate);
            throw new ELogoApiException(
                $"Failed to retrieve invoices from eLogo API: {ex.Message}",
                operationName: nameof(GetInvoicesAsync),
                innerException: ex);
        }
    }

    public async Task<byte[]> GetInvoicePdfAsync(
        string ettn,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ettn);
        var encodedEttn = Uri.EscapeDataString(ettn.Trim());
        var requestUri = $"/api/v1/einvoices/{encodedEttn}/pdf";

        try
        {
            using var response = await _httpClient.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new InvoiceNotFoundException(ettn);
            }

            await EnsureSuccessResponseAsync(response, nameof(GetInvoicePdfAsync), ettn, cancellationToken)
                .ConfigureAwait(false);

            return await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not DomainException and not OperationCanceledException)
        {
            _logger.LogError(ex, "Error while downloading PDF from eLogo API for ETTN: {Ettn}.", ettn);
            throw new ELogoApiException(
                $"Failed to download PDF for ETTN '{ettn}' from eLogo API: {ex.Message}",
                operationName: nameof(GetInvoicePdfAsync),
                innerException: ex);
        }
    }

    public async Task<bool> MarkAsUnreadAsync(
        string ettn,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ettn);
        var encodedEttn = Uri.EscapeDataString(ettn.Trim());
        var requestUri = $"/api/v1/einvoices/{encodedEttn}/status";

        var requestBody = new ELogoUpdateStatusApiRequest
        {
            Ettn = ettn.Trim(),
            TargetStatus = "UNREAD",
            MoveToInbox = true
        };

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(requestUri, requestBody, JsonOptions, cancellationToken)
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new InvoiceNotFoundException(ettn);
            }

            await EnsureSuccessResponseAsync(response, nameof(MarkAsUnreadAsync), ettn, cancellationToken)
                .ConfigureAwait(false);

            var result = await response.Content
                .ReadFromJsonAsync<ELogoOperationResultDto>(JsonOptions, cancellationToken)
                .ConfigureAwait(false);

            return result?.Success ?? true;
        }
        catch (Exception ex) when (ex is not DomainException and not OperationCanceledException)
        {
            _logger.LogError(ex, "Error while marking ETTN {Ettn} as unread on eLogo API.", ettn);
            throw new ELogoApiException(
                $"Failed to mark invoice '{ettn}' as unread on eLogo API: {ex.Message}",
                operationName: nameof(MarkAsUnreadAsync),
                innerException: ex);
        }
    }

    private async Task EnsureSuccessResponseAsync(
        HttpResponseMessage response,
        string operationName,
        string? ettn = null,
        CancellationToken cancellationToken = default)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var errorBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogError(
            "eLogo API call {OperationName} failed with HTTP {StatusCode} (ETTN: {Ettn}). Body: {ErrorBody}",
            operationName,
            (int)response.StatusCode,
            ettn ?? "N/A",
            errorBody);

        throw new ELogoApiException(
            $"eLogo API returned HTTP {(int)response.StatusCode} ({response.ReasonPhrase}) during '{operationName}'.",
            operationName: operationName,
            httpStatusCode: (int)response.StatusCode);
    }

    private sealed record ELogoInvoiceListApiResponse
    {
        public IReadOnlyList<ELogoInvoiceItemDto> Invoices { get; init; } = [];
    }

    private sealed record ELogoInvoiceItemDto
    {
        public required string Ettn { get; init; }
        public required string InvoiceNumber { get; init; }
        public required string SenderTaxNumber { get; init; }
        public required string SenderTitle { get; init; }
        public required DateTime IssueDate { get; init; }
        public required decimal PayableAmount { get; init; }
        public decimal TaxExclusiveAmount { get; init; }
        public decimal TaxAmount { get; init; }
        public string CurrencyCode { get; init; } = "TRY";
        public string? ProfileId { get; init; }
        public string? InvoiceType { get; init; }
        public InvoiceIntegrationStatus Status { get; init; } = InvoiceIntegrationStatus.ReadByErp;
    }

    private sealed record ELogoUpdateStatusApiRequest
    {
        public required string Ettn { get; init; }
        public required string TargetStatus { get; init; }
        public required bool MoveToInbox { get; init; }
    }

    private sealed record ELogoOperationResultDto
    {
        public bool Success { get; init; }
        public string? Message { get; init; }
    }
}
