using AutoZapSaaS.Application.Common.Interfaces;
using AutoZapSaaS.Infrastructure.Clients;
using AutoZapSaaS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AutoZapSaaS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly("AutoZapSaaS.Infrastructure")));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        services.Configure<EvolutionApiSettings>(configuration.GetSection("EvolutionApi"));
        services.Configure<AsaasSettings>(configuration.GetSection("Asaas"));

        services.AddHttpClient<IAsaasClient, AsaasClient>(client =>
        {
            var settings = configuration.GetSection("Asaas").Get<AsaasSettings>() ?? new AsaasSettings();

            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            if (!string.IsNullOrWhiteSpace(settings.ApiKey))
                client.DefaultRequestHeaders.Add("access_token", settings.ApiKey);
        });

        services.AddHttpClient<IEvolutionApiClient, EvolutionApiClient>(client =>
        {
            var settings = configuration.GetSection("EvolutionApi").Get<EvolutionApiSettings>()
                ?? new EvolutionApiSettings();

            client.BaseAddress = new Uri(settings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            // Configurado uma unica vez. Mutar DefaultRequestHeaders a cada chamada
            // nao e thread-safe: o HttpClient e compartilhado entre requisicoes.
            if (!string.IsNullOrWhiteSpace(settings.ApiKey))
                client.DefaultRequestHeaders.Add("apikey", settings.ApiKey);
        });

        return services;
    }
}
