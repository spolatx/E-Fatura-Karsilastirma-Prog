using System.Text.RegularExpressions;
using Dapper;
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
    private readonly ILogger<LogoDapperRepository> _logger;

    public LogoDapperRepository(
        IConfiguration configuration,
        IOptions<LogoDatabaseOptions> options,
        ILogger<LogoDapperRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _connectionString = !string.IsNullOrWhiteSpace(_options.ConnectionString)
            ? _options.ConnectionString
            : configuration.GetConnectionString("LogoDatabase") ?? string.Empty;

        ValidateTableIdentifiers(_options.FirmNumber, _options.PeriodNumber);
    }

    public async Task<HashSet<string>> GetExistingEttnsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new LogoDbException(
                "Logo ERP SQL Server connection string is not configured (Check 'ConnectionStrings:LogoDatabase' or 'LogoDatabase:ConnectionString').",
                databaseOperation: nameof(GetExistingEttnsAsync));
        }

        var stficheTableName = $"LG_{_options.FirmNumber}_{_options.PeriodNumber}_STFICHE";
        var invoiceTableName = $"LG_{_options.FirmNumber}_{_options.PeriodNumber}_INVOICE";

        // Queries ETTN values from both LG_XXX_XX_STFICHE (TRACKNR) and LG_XXX_XX_INVOICE (TRACKNR / GUID)
        // using WITH (NOLOCK) so active Logo ERP transactions are never blocked.
        var sql = $"""
            SELECT DISTINCT
                LTRIM(RTRIM(STF.[TRACKNR])) AS Ettn
            FROM [{stficheTableName}] AS STF WITH (NOLOCK)
            WHERE STF.[CANCELLED] = 0
              AND STF.[DATE_] >= @StartDate
              AND STF.[DATE_] <= @EndDate
              AND STF.[TRACKNR] IS NOT NULL
              AND LEN(LTRIM(RTRIM(STF.[TRACKNR]))) >= 32

            UNION

            SELECT DISTINCT
                LTRIM(RTRIM(COALESCE(NULLIF(LTRIM(RTRIM(INV.[TRACKNR])), ''), NULLIF(LTRIM(RTRIM(INV.[GUID])), '')))) AS Ettn
            FROM [{invoiceTableName}] AS INV WITH (NOLOCK)
            WHERE INV.[CANCELLED] = 0
              AND INV.[DATE_] >= @StartDate
              AND INV.[DATE_] <= @EndDate
              AND (
                  (INV.[TRACKNR] IS NOT NULL AND LEN(LTRIM(RTRIM(INV.[TRACKNR]))) >= 32)
                  OR (INV.[GUID] IS NOT NULL AND LEN(LTRIM(RTRIM(INV.[GUID]))) >= 32)
              );
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

            var ettns = await connection.QueryAsync<string>(command).ConfigureAwait(false);

            var resultHashSet = new HashSet<string>(
                ettns.Where(ettn => !string.IsNullOrWhiteSpace(ettn))
                     .Select(ettn => ettn.Trim().Trim('{', '}')),
                StringComparer.OrdinalIgnoreCase);

            _logger.LogInformation(
                "Retrieved {Count} distinct ETTNs from Logo ERP tables [{StficheTable}] / [{InvoiceTable}] for range {StartDate:yyyy-MM-dd} - {EndDate:yyyy-MM-dd}.",
                resultHashSet.Count,
                stficheTableName,
                invoiceTableName,
                startDate,
                endDate);

            return resultHashSet;
        }
        catch (SqlException ex)
        {
            _logger.LogError(
                ex,
                "SQL error #{SqlErrorNumber} occurred while querying Logo ERP tables [{StficheTable}] / [{InvoiceTable}].",
                ex.Number,
                stficheTableName,
                invoiceTableName);

            throw new LogoDbException(
                $"Database error occurred while reading ETTNs from Logo ERP: {ex.Message}",
                databaseOperation: nameof(GetExistingEttnsAsync),
                sqlErrorNumber: ex.Number,
                innerException: ex);
        }
        catch (Exception ex) when (ex is not LogoDbException and not OperationCanceledException)
        {
            _logger.LogError(ex, "Unexpected error while querying Logo ERP database.");

            throw new LogoDbException(
                $"Unexpected database error while querying Logo ERP: {ex.Message}",
                databaseOperation: nameof(GetExistingEttnsAsync),
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

