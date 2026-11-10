using System.Net.Http.Headers;
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

        services.AddHttpClient<IELogoIntegrationService, ELogoIntegrationService>((serviceProvider, client) =>
        {
            var eLogoOptions = configuration
                .GetSection(ELogoOptions.SectionName)
                .Get<ELogoOptions>() ?? new ELogoOptions();

            if (!string.IsNullOrWhiteSpace(eLogoOptions.BaseUrl))
            {
                client.BaseAddress = new Uri(eLogoOptions.BaseUrl.TrimEnd('/'));
            }

            client.Timeout = TimeSpan.FromSeconds(
                eLogoOptions.TimeoutSeconds > 0 ? eLogoOptions.TimeoutSeconds : 90);

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            if (!string.IsNullOrWhiteSpace(eLogoOptions.ApiKey))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", eLogoOptions.ApiKey);
            }
        });

        return services;
    }
}
