using System.Security.Cryptography;
using AutoZapSaaS.Domain.Enums;

namespace AutoZapSaaS.Domain.Entities;

/// <summary>
/// Credenciais de webhook de um tenant para uma plataforma. O <see cref="Token"/> identifica
/// o tenant na URL pública; o <see cref="Secret"/> valida a assinatura do corpo da requisição.
/// Sem os dois, um webhook não é aceito — é isso que impede um terceiro de disparar
/// mensagens no nome de outra empresa.
/// </summary>
public class WebhookIntegration : Entity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public WebhookPlatform Platform { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public string Secret { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime? LastReceivedAt { get; private set; }

    public virtual Tenant? Tenant { get; private set; }

    private WebhookIntegration() { }

    public WebhookIntegration(Guid tenantId, WebhookPlatform platform, string secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
            throw new ArgumentException("Secret é obrigatório.", nameof(secret));

        TenantId = tenantId;
        Platform = platform;
        Token = GenerateToken();
        Secret = secret;
        IsActive = true;
    }

    public void RotateToken()
    {
        Token = GenerateToken();
        SetUpdatedAt();
    }

    public void SetSecret(string secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
            throw new ArgumentException("Secret é obrigatório.", nameof(secret));

        Secret = secret;
        SetUpdatedAt();
    }

    public void MarkReceived()
    {
        LastReceivedAt = DateTime.UtcNow;
        SetUpdatedAt();
    }

    public void Deactivate()
    {
        IsActive = false;
        SetUpdatedAt();
    }

    /// <summary>32 bytes de entropia, base64url — inadivinhável por força bruta.</summary>
    private static string GenerateToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
}
