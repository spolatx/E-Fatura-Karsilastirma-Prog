using InvoiceComparison.Application.DTOs;

namespace InvoiceComparison.Application.Interfaces;

public interface ICompanyProvider
{
    IReadOnlyList<CompanyDto> GetCompanies();
}

