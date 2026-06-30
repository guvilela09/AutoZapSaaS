using AutoZapSaaS.Domain.Enums;

namespace AutoZapSaaS.Domain.Entities;

public class MessageTemplate : Entity
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; }
    public EventType EventType { get; private set; }
    public string Body { get; private set; }
    public bool IsActive { get; private set; }
    public virtual Tenant Tenant { get; private set; }

    public MessageTemplate() { }

    public MessageTemplate(Guid tenantId, string name, EventType eventType, string body)
    {
        TenantId = tenantId;
        Name = name;
        EventType = eventType;
        Body = body;
        IsActive = true;
    }

    public void SetBody(string body)
    {
        Body = body;
        SetUpdatedAt();
    }

    public void Deactivate()
    {
        IsActive = false;
        SetUpdatedAt();
    }

    public void Activate()
    {
        IsActive = true;
        SetUpdatedAt();
    }

    public string Render(string customerName, string eventDetail)
    {
        return Body
            .Replace("{{nome}}", customerName ?? "Cliente")
            .Replace("{{detalhe}}", eventDetail ?? string.Empty);
    }
}
