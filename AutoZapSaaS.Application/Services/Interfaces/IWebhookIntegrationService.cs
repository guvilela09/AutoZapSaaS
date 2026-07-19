using AutoZapSaaS.Application.DTOs;

namespace AutoZapSaaS.Application.Services.Interfaces;

public interface IWebhookIntegrationService
{
    Task<WebhookIntegrationResponse> CreateAsync(Guid tenantId, CreateWebhookIntegrationRequest request);
    Task<List<WebhookIntegrationResponse>> GetByTenantAsync(Guid tenantId);
    Task<WebhookIntegrationResponse?> RotateTokenAsync(Guid id);
    Task<WebhookIntegrationResponse?> UpdateSecretAsync(Guid id, UpdateWebhookSecretRequest request);
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// Resolve o tenant a partir do token público da URL. Roda sem tenant na requisição,
    /// portanto ignora os query filters de propósito — é o único ponto de entrada
    /// não autenticado que pode fazer isso.
    /// </summary>
    Task<WebhookIntegrationLookup?> ResolveByTokenAsync(string token);

    Task MarkReceivedAsync(Guid integrationId);
}
