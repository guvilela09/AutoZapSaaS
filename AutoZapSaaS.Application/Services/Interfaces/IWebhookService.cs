using System.Text.Json;
using AutoZapSaaS.Application.DTOs;

namespace AutoZapSaaS.Application.Services.Interfaces;

public interface IWebhookService
{
    Task<WebhookEventResponse> ProcessKiwifyAsync(Guid tenantId, JsonElement payload);
    Task<WebhookEventResponse> ProcessHotmartAsync(Guid tenantId, JsonElement payload);
    Task<WebhookEventResponse> ProcessNuvemshopAsync(Guid tenantId, JsonElement payload);
}
