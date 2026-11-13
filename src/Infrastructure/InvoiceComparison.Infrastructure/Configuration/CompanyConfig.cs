namespace InvoiceComparison.Infrastructure.Configuration;

public sealed class CompanyConfig
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string LogoFirmNumber { get; set; }
    public string LogoPeriodNumber { get; set; } = "01";
    public required string ELogoUserName { get; set; }
    public required string ELogoPassword { get; set; }
}

