namespace InvoiceComparison.Infrastructure.Configuration;

public sealed class ELogoOptions
{
    public const string SectionName = "ELogo";

    public string BaseUrl { get; set; } = "https://pb.elogo.com.tr/PostBoxService.svc";
    public string ServiceUrl { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 90;
    public string DocumentType { get; set; } = "EINVOICE";
}
