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

    public async Task<IReadOnlyList<InvoiceIdentityDto>> GetExistingInvoicesAsync(
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
                databaseOperation: nameof(GetExistingInvoicesAsync));
        }

        var company = _companyConfigService.GetCompany(companyId);
        ValidateTableIdentifiers(company.LogoFirmNumber, company.LogoPeriodNumber);

        var stficheTableName = $"LG_{company.LogoFirmNumber}_{company.LogoPeriodNumber}_STFICHE";
        var invoiceTableName = $"LG_{company.LogoFirmNumber}_{company.LogoPeriodNumber}_INVOICE";

        // Queries both FICHENO and TRACKNR (ETTN) from LG_XXX_XX_STFICHE and LG_XXX_XX_INVOICE
        // filters for incoming invoice TRCODEs (1=Satınalma, 2=Perakende İade, 3=Toptan Satış İade, 4=Alınan Hizmet, 13=Fiyat Farkı, 14=Alınan Hizmet vb.)
        // filtering by official issue date (DATE_) and ordered by DATE_ DESC, TIME_ DESC
        // and WITH (NOLOCK) so active Logo transactions are never blocked.
        var sql = $"""
            SELECT 
                LTRIM(RTRIM(T.[FicheNo])) AS FicheNo,
                LTRIM(RTRIM(T.[Ettn])) AS Ettn
            FROM
            (
                SELECT 
                    STF.[FICHENO] AS FicheNo,
                    STF.[TRACKNR] AS Ettn,
                    STF.[DATE_] AS InvoiceDate,
                    STF.[FTIME] AS InvoiceTime
                FROM [{stficheTableName}] AS STF WITH (NOLOCK)
                WHERE STF.[CANCELLED] = 0
                  AND STF.[DATE_] >= @StartDate
                  AND STF.[DATE_] <= @EndDate
                  AND STF.[TRCODE] IN (1, 2, 3, 4, 13, 14, 26)

                UNION ALL

                SELECT 
                    INV.[FICHENO] AS FicheNo,
                    INV.[TRACKNR] AS Ettn,
                    INV.[DATE_] AS InvoiceDate,
                    INV.[TIME_] AS InvoiceTime
                FROM [{invoiceTableName}] AS INV WITH (NOLOCK)
                WHERE INV.[CANCELLED] = 0
                  AND INV.[DATE_] >= @StartDate
                  AND INV.[DATE_] <= @EndDate
                  AND INV.[TRCODE] IN (1, 2, 3, 4, 13, 14, 26)
            ) AS T
            ORDER BY T.[InvoiceDate] DESC, T.[InvoiceTime] DESC;
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
                "Retrieved {Count} distinct invoices from Logo ERP tables [{StficheTable}] / [{InvoiceTable}] for company '{CompanyId}' ({Range}).",
                invoices.Count,
                stficheTableName,
                invoiceTableName,
                companyId,
                $"{startDate:yyyy-MM-dd} - {endDate:yyyy-MM-dd}");

            return invoices;
        }
        catch (SqlException ex)
        {
            _logger.LogError(
                ex,
                "SQL error #{SqlErrorNumber} occurred while querying Logo ERP tables [{StficheTable}] / [{InvoiceTable}] for company '{CompanyId}'.",
                ex.Number,
                stficheTableName,
                invoiceTableName,
                companyId);

            throw new LogoDbException(
                $"Database error occurred while reading invoices from Logo ERP: {ex.Message}",
                databaseOperation: nameof(GetExistingInvoicesAsync),
                sqlErrorNumber: ex.Number,
                innerException: ex);
        }
        catch (Exception ex) when (ex is not LogoDbException and not OperationCanceledException)
        {
            _logger.LogError(ex, "Unexpected error while querying Logo ERP database for company '{CompanyId}'.", companyId);

            throw new LogoDbException(
                $"Unexpected database error while querying Logo ERP: {ex.Message}",
                databaseOperation: nameof(GetExistingInvoicesAsync),
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
