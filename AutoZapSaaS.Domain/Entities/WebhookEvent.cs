using System.Security.Cryptography;
using System.Text;
using AutoZapSaaS.Domain.Enums;

namespace AutoZapSaaS.Domain.Entities;

public class WebhookEvent : Entity
{
    public Guid? TenantId { get; private set; }
    public WebhookPlatform Platform { get; private set; }
    public EventType EventType { get; private set; }
    public string RawPayload { get; private set; }
    public bool Processed { get; private set; }
    public string Error { get; private set; }

    /// <summary>
    /// Hash do corpo cru, usado para reconhecer reentrega. Kiwify, Hotmart e
    /// Nuvemshop reenviam o mesmo webhook em timeout ou falha, e sem isso o
    /// cliente final recebe a mesma mensagem varias vezes.
    /// </summary>
    public string PayloadHash { get; private set; } = string.Empty;

    public WebhookEvent() { }

    public WebhookEvent(Guid? tenantId, WebhookPlatform platform, EventType eventType, string rawPayload)
    {
        TenantId = tenantId;
        Platform = platform;
        EventType = eventType;
        RawPayload = rawPayload;
        PayloadHash = CalcularHash(rawPayload);
        Processed = false;
        Error = string.Empty;
    }

    public static string CalcularHash(string rawPayload) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawPayload ?? string.Empty)));

    public void MarkProcessed()
    {
        Processed = true;
        SetUpdatedAt();
    }

    public void MarkFailed(string error)
    {
        Processed = false;
        Error = error;
        SetUpdatedAt();
    }
}
