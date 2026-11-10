namespace InvoiceComparison.Infrastructure.Configuration;

public sealed class ELogoOptions
{
    public const string SectionName = "ELogo";

    public string BaseUrl { get; set; } = "https://api.elogo.com.tr";
    public string ApiKey { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 90;
}
