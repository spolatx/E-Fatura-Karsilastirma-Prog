namespace InvoiceComparison.Infrastructure.Configuration;

public sealed class LogoDatabaseOptions
{
    public const string SectionName = "LogoDatabase";

    public string ConnectionString { get; set; } = string.Empty;
    public string FirmNumber { get; set; } = "001";
    public string PeriodNumber { get; set; } = "01";
    public int CommandTimeoutSeconds { get; set; } = 60;
}
