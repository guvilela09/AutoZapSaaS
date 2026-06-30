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

        services.AddHttpClient<IEvolutionApiClient, EvolutionApiClient>(client =>
        {
            var settings = configuration.GetSection("EvolutionApi").Get<EvolutionApiSettings>()
                ?? new EvolutionApiSettings();

            client.BaseAddress = new Uri(settings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        });

        return services;
    }
}
