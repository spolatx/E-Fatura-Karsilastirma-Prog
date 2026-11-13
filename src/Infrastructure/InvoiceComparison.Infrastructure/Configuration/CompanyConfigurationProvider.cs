using InvoiceComparison.Application.DTOs;
using InvoiceComparison.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace InvoiceComparison.Infrastructure.Configuration;

public interface ICompanyConfigService : ICompanyProvider
{
    CompanyConfig GetCompany(string companyId);
}

public sealed class CompanyConfigurationProvider : ICompanyConfigService
{
    private readonly IConfiguration _configuration;

    public CompanyConfigurationProvider(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public IReadOnlyList<CompanyDto> GetCompanies()
    {
        var companies = _configuration
            .GetSection("Companies")
            .Get<List<CompanyConfig>>() ?? [];

        return companies
            .Select(c => new CompanyDto(c.Id, c.Name))
            .ToList();
    }

    public CompanyConfig GetCompany(string companyId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(companyId);

        var companies = _configuration
            .GetSection("Companies")
            .Get<List<CompanyConfig>>() ?? [];

        var company = companies.FirstOrDefault(c => string.Equals(c.Id, companyId.Trim(), StringComparison.OrdinalIgnoreCase));
        if (company is null)
        {
            throw new ArgumentException($"Company with ID '{companyId}' was not found in configuration.", nameof(companyId));
        }

        return company;
    }
}

