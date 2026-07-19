using AutoZapSaaS.Application.Common;
using AutoZapSaaS.Application.Common.Interfaces;
using AutoZapSaaS.Application.DTOs;
using AutoZapSaaS.Application.Services.Interfaces;
using AutoZapSaaS.Domain.Entities;
using AutoZapSaaS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoZapSaaS.Application.Services.Implementations;

public class WebhookIntegrationService : IWebhookIntegrationService
{
    private readonly IApplicationDbContext _context;
    private readonly AppSettings _appSettings;
    private readonly ILogger<WebhookIntegrationService> _logger;

    public WebhookIntegrationService(
        IApplicationDbContext context,
        IOptions<AppSettings> appSettings,
        ILogger<WebhookIntegrationService> logger)
    {
        _context = context;
        _appSettings = appSettings.Value;
        _logger = logger;
    }

    public async Task<WebhookIntegrationResponse> CreateAsync(Guid tenantId, CreateWebhookIntegrationRequest request)
    {
        if (!Enum.TryParse<WebhookPlatform>(request.Platform, true, out var platform))
            throw new ArgumentException($"Plataforma inválida: {request.Platform}");

        var existing = await _context.WebhookIntegrations
            .FirstOrDefaultAsync(w => w.Platform == platform);

        if (existing is not null)
            throw new InvalidOperationException($"Já existe uma integração de {platform} para esta conta.");

        var integration = new WebhookIntegration(tenantId, platform, request.Secret);
        _context.WebhookIntegrations.Add(integration);
        await _context.SaveChangesAsync(CancellationToken.None);

        _logger.LogInformation("Integração de webhook criada: {Platform} para tenant {TenantId}", platform, tenantId);
        return ToResponse(integration);
    }

    public async Task<List<WebhookIntegrationResponse>> GetByTenantAsync(Guid tenantId)
    {
        var integrations = await _context.WebhookIntegrations
            .OrderBy(w => w.Platform)
            .ToListAsync();

        return integrations.Select(ToResponse).ToList();
    }

    public async Task<WebhookIntegrationResponse?> RotateTokenAsync(Guid id)
    {
        var integration = await _context.WebhookIntegrations.FirstOrDefaultAsync(w => w.Id == id);
        if (integration is null) return null;

        integration.RotateToken();
        await _context.SaveChangesAsync(CancellationToken.None);

        _logger.LogInformation("Token de webhook rotacionado: {IntegrationId}", id);
        return ToResponse(integration);
    }

    public async Task<WebhookIntegrationResponse?> UpdateSecretAsync(Guid id, UpdateWebhookSecretRequest request)
    {
        var integration = await _context.WebhookIntegrations.FirstOrDefaultAsync(w => w.Id == id);
        if (integration is null) return null;

        integration.SetSecret(request.Secret);
        await _context.SaveChangesAsync(CancellationToken.None);
        return ToResponse(integration);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var integration = await _context.WebhookIntegrations.FirstOrDefaultAsync(w => w.Id == id);
        if (integration is null) return false;

        _context.WebhookIntegrations.Remove(integration);
        await _context.SaveChangesAsync(CancellationToken.None);
        return true;
    }

    public async Task<WebhookIntegrationLookup?> ResolveByTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        // Sem tenant na requisição ainda — é justamente este lookup que o descobre.
        var integration = await _context.WebhookIntegrations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.Token == token && w.IsActive);

        return integration is null
            ? null
            : new WebhookIntegrationLookup(integration.Id, integration.TenantId, integration.Platform, integration.Secret);
    }

    public async Task MarkReceivedAsync(Guid integrationId)
    {
        var integration = await _context.WebhookIntegrations
            .FirstOrDefaultAsync(w => w.Id == integrationId);

        if (integration is null) return;

        integration.MarkReceived();
        await _context.SaveChangesAsync(CancellationToken.None);
    }

    private WebhookIntegrationResponse ToResponse(WebhookIntegration integration)
    {
        var baseUrl = _appSettings.PublicBaseUrl.TrimEnd('/');
        var platform = integration.Platform.ToString().ToLowerInvariant();

        return new WebhookIntegrationResponse(
            integration.Id,
            integration.Platform.ToString(),
            $"{baseUrl}/api/webhooks/{platform}/{integration.Token}",
            integration.IsActive,
            integration.LastReceivedAt,
            integration.CreatedAt);
    }
}
