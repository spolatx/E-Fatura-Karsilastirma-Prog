using InvoiceComparison.Application.Interfaces;
using InvoiceComparison.Infrastructure.Configuration;
using InvoiceComparison.Infrastructure.ExternalServices;
using InvoiceComparison.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceComparison.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<LogoDatabaseOptions>(
            configuration.GetSection(LogoDatabaseOptions.SectionName));

        services.Configure<ELogoOptions>(
            configuration.GetSection(ELogoOptions.SectionName));

        services.AddScoped<ILogoDatabaseRepository, LogoDapperRepository>();
        services.AddScoped<IELogoIntegrationService, ELogoIntegrationService>();

        return services;
    }
}
