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

    public WebhookEvent() { }

    public WebhookEvent(Guid? tenantId, WebhookPlatform platform, EventType eventType, string rawPayload)
    {
        TenantId = tenantId;
        Platform = platform;
        EventType = eventType;
        RawPayload = rawPayload;
        Processed = false;
        Error = string.Empty;
    }

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
