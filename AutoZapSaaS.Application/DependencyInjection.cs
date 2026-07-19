using System.Reflection;
using AutoZapSaaS.Application.Common;
using AutoZapSaaS.Application.Common.Interfaces;
using AutoZapSaaS.Application.Services.Implementations;
using AutoZapSaaS.Application.Services.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AutoZapSaaS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAutoMapper(Assembly.GetExecutingAssembly());
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.Configure<AppSettings>(configuration.GetSection("App"));

        services.AddSingleton<IWebhookSignatureValidator, WebhookSignatureValidator>();
        services.AddScoped<IWebhookIntegrationService, WebhookIntegrationService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<IInstanceService, InstanceService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IWebhookService, WebhookService>();
        services.AddScoped<IWhatsAppService, WhatsAppService>();
        services.AddScoped<IMessageTemplateService, MessageTemplateService>();

        return services;
    }
}
