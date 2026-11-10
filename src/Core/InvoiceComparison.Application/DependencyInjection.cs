using InvoiceComparison.Application.Interfaces;
using InvoiceComparison.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceComparison.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IMissingInvoiceDetectorService, MissingInvoiceDetectorService>();
        return services;
    }
}

