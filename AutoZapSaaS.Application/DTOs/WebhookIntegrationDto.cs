namespace AutoZapSaaS.Application.DTOs;

public record CreateWebhookIntegrationRequest(string Platform, string Secret);

public record UpdateWebhookSecretRequest(string Secret);

/// <summary>
/// O Secret nunca volta nesta resposta — só a URL que o tenant cadastra na plataforma.
/// </summary>
public record WebhookIntegrationResponse(
    Guid Id,
    string Platform,
    string WebhookUrl,
    bool IsActive,
    DateTime? LastReceivedAt,
    DateTime CreatedAt);

/// <summary>Resultado interno da resolução de um token de webhook. Nunca sai da API.</summary>
public record WebhookIntegrationLookup(
    Guid IntegrationId,
    Guid TenantId,
    Domain.Enums.WebhookPlatform Platform,
    string Secret);
