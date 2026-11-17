using System.Text.RegularExpressions;
using Dapper;
using InvoiceComparison.Application.DTOs;
using InvoiceComparison.Application.Interfaces;
using InvoiceComparison.Domain.Exceptions;
using InvoiceComparison.Infrastructure.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InvoiceComparison.Infrastructure.Persistence;

public sealed partial class LogoDapperRepository : ILogoDatabaseRepository
{
    private readonly string _connectionString;
    private readonly LogoDatabaseOptions _options;
    private readonly ICompanyConfigService _companyConfigService;
    private readonly ILogger<LogoDapperRepository> _logger;

    public LogoDapperRepository(
        IConfiguration configuration,
        IOptions<LogoDatabaseOptions> options,
        ICompanyConfigService companyConfigService,
        ILogger<LogoDapperRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _companyConfigService = companyConfigService ?? throw new ArgumentNullException(nameof(companyConfigService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _connectionString = !string.IsNullOrWhiteSpace(_options.ConnectionString)
            ? _options.ConnectionString
            : configuration.GetConnectionString("LogoDatabase") ?? string.Empty;
    }

    public async Task<IReadOnlyList<InvoiceIdentityDto>> GetPurchaseInvoicesAsync(
        string companyId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(companyId);

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new LogoDbException(
                "Logo ERP SQL Server connection string is not configured (Check 'ConnectionStrings:LogoDatabase').",
                databaseOperation: nameof(GetPurchaseInvoicesAsync));
        }

        var company = _companyConfigService.GetCompany(companyId);
        ValidateTableIdentifiers(company.LogoFirmNumber, company.LogoPeriodNumber);

        var invoiceTableName = $"LG_{company.LogoFirmNumber}_{company.LogoPeriodNumber}_INVOICE";
        var clcardTableName = $"LG_{company.LogoFirmNumber}_CLCARD";

        // Satın alma (Gelen) faturaları: TRCODE IN (1, 2, 3, 4, 13)
        // Join with CLCARD to get supplier details (TAXNR/TCKNO, TITLE)
        var sql = $"""
            SELECT 
                LTRIM(RTRIM(INV.[FICHENO])) AS FicheNo,
                LTRIM(RTRIM(INV.[TRACKNR])) AS Ettn,
                LTRIM(RTRIM(COALESCE(NULLIF(CLC.[TAXNR], ''), CLC.[TCKNO]))) AS TaxNumber,
                LTRIM(RTRIM(CLC.[DEFINITION_])) AS ClientTitle,
                CAST(INV.[EINVOICE] AS INT) AS EInvoiceType
            FROM [{invoiceTableName}] AS INV WITH (NOLOCK)
            LEFT JOIN [{clcardTableName}] AS CLC WITH (NOLOCK) ON CLC.[LOGICALREF] = INV.[CLIENTREF]
            WHERE INV.[CANCELLED] = 0
              AND INV.[DATE_] >= @StartDate
              AND INV.[DATE_] <= @EndDate
              AND INV.[TRCODE] IN (1, 2, 3, 4, 13)
            ORDER BY INV.[DATE_] DESC, INV.[TIME_] DESC;
            """;

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var command = new CommandDefinition(
                commandText: sql,
                parameters: new
                {
                    StartDate = startDate.Date,
                    EndDate = endDate.Date.AddDays(1).AddTicks(-1)
                },
                commandTimeout: _options.CommandTimeoutSeconds,
                cancellationToken: cancellationToken);

            var invoices = (await connection.QueryAsync<InvoiceIdentityDto>(command).ConfigureAwait(false)).ToList();

            _logger.LogInformation(
                "Retrieved {Count} purchase invoices from Logo ERP [{InvoiceTable}] for company '{CompanyId}' ({Range}).",
                invoices.Count,
                invoiceTableName,
                companyId,
                $"{startDate:yyyy-MM-dd} - {endDate:yyyy-MM-dd}");

            return invoices;
        }
        catch (SqlException ex)
        {
            _logger.LogError(
                ex,
                "SQL error #{SqlErrorNumber} occurred while querying purchase invoices from Logo ERP [{InvoiceTable}] for company '{CompanyId}'.",
                ex.Number,
                invoiceTableName,
                companyId);

            throw new LogoDbException(
                $"Database error occurred while reading purchase invoices from Logo ERP: {ex.Message}",
                databaseOperation: nameof(GetPurchaseInvoicesAsync),
                sqlErrorNumber: ex.Number,
                innerException: ex);
        }
        catch (Exception ex) when (ex is not LogoDbException and not OperationCanceledException)
        {
            _logger.LogError(ex, "Unexpected error while querying purchase invoices from Logo ERP for company '{CompanyId}'.", companyId);

            throw new LogoDbException(
                $"Unexpected database error while querying purchase invoices from Logo ERP: {ex.Message}",
                databaseOperation: nameof(GetPurchaseInvoicesAsync),
                innerException: ex);
        }
    }

    public async Task<IReadOnlyList<InvoiceIdentityDto>> GetSalesInvoicesAsync(
        string companyId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(companyId);

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new LogoDbException(
                "Logo ERP SQL Server connection string is not configured (Check 'ConnectionStrings:LogoDatabase').",
                databaseOperation: nameof(GetSalesInvoicesAsync));
        }

        var company = _companyConfigService.GetCompany(companyId);
        ValidateTableIdentifiers(company.LogoFirmNumber, company.LogoPeriodNumber);

        var invoiceTableName = $"LG_{company.LogoFirmNumber}_{company.LogoPeriodNumber}_INVOICE";
        var clcardTableName = $"LG_{company.LogoFirmNumber}_CLCARD";

        // Satış (Giden) faturaları: TRCODE IN (6, 7, 8, 9, 14)
        // Join with CLCARD to get customer details (TAXNR/TCKNO, TITLE) and EINVOICE (1: e-Fatura, 2: e-Arşiv)
        var sql = $"""
            SELECT 
                LTRIM(RTRIM(INV.[FICHENO])) AS FicheNo,
                LTRIM(RTRIM(INV.[TRACKNR])) AS Ettn,
                LTRIM(RTRIM(COALESCE(NULLIF(CLC.[TAXNR], ''), CLC.[TCKNO]))) AS TaxNumber,
                LTRIM(RTRIM(CLC.[DEFINITION_])) AS ClientTitle,
                CAST(INV.[EINVOICE] AS INT) AS EInvoiceType
            FROM [{invoiceTableName}] AS INV WITH (NOLOCK)
            LEFT JOIN [{clcardTableName}] AS CLC WITH (NOLOCK) ON CLC.[LOGICALREF] = INV.[CLIENTREF]
            WHERE INV.[CANCELLED] = 0
              AND INV.[DATE_] >= @StartDate
              AND INV.[DATE_] <= @EndDate
              AND INV.[TRCODE] IN (6, 7, 8, 9, 14)
            ORDER BY INV.[DATE_] DESC, INV.[TIME_] DESC;
            """;

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var command = new CommandDefinition(
                commandText: sql,
                parameters: new
                {
                    StartDate = startDate.Date,
                    EndDate = endDate.Date.AddDays(1).AddTicks(-1)
                },
                commandTimeout: _options.CommandTimeoutSeconds,
                cancellationToken: cancellationToken);

            var invoices = (await connection.QueryAsync<InvoiceIdentityDto>(command).ConfigureAwait(false)).ToList();

            _logger.LogInformation(
                "Retrieved {Count} sales invoices from Logo ERP [{InvoiceTable}] for company '{CompanyId}' ({Range}).",
                invoices.Count,
                invoiceTableName,
                companyId,
                $"{startDate:yyyy-MM-dd} - {endDate:yyyy-MM-dd}");

            return invoices;
        }
        catch (SqlException ex)
        {
            _logger.LogError(
                ex,
                "SQL error #{SqlErrorNumber} occurred while querying sales invoices from Logo ERP [{InvoiceTable}] for company '{CompanyId}'.",
                ex.Number,
                invoiceTableName,
                companyId);

            throw new LogoDbException(
                $"Database error occurred while reading sales invoices from Logo ERP: {ex.Message}",
                databaseOperation: nameof(GetSalesInvoicesAsync),
                sqlErrorNumber: ex.Number,
                innerException: ex);
        }
        catch (Exception ex) when (ex is not LogoDbException and not OperationCanceledException)
        {
            _logger.LogError(ex, "Unexpected error while querying sales invoices from Logo ERP for company '{CompanyId}'.", companyId);

            throw new LogoDbException(
                $"Unexpected database error while querying sales invoices from Logo ERP: {ex.Message}",
                databaseOperation: nameof(GetSalesInvoicesAsync),
                innerException: ex);
        }
    }

    private static void ValidateTableIdentifiers(string firmNumber, string periodNumber)
    {
        if (!FirmNumberRegex().IsMatch(firmNumber ?? string.Empty))
        {
            throw new ArgumentException(
                $"Invalid Logo FirmNumber '{firmNumber}'. Must be a 3-digit number (e.g. '001').",
                nameof(firmNumber));
        }

        if (!PeriodNumberRegex().IsMatch(periodNumber ?? string.Empty))
        {
            throw new ArgumentException(
                $"Invalid Logo PeriodNumber '{periodNumber}'. Must be a 2-digit number (e.g. '01').",
                nameof(periodNumber));
        }
    }

    [GeneratedRegex(@"^\d{3}$", RegexOptions.Compiled)]
    private static partial Regex FirmNumberRegex();

    [GeneratedRegex(@"^\d{2}$", RegexOptions.Compiled)]
    private static partial Regex PeriodNumberRegex();
}
