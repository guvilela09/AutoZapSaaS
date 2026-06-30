using AutoZapSaaS.Domain.Enums;

namespace AutoZapSaaS.Domain.Entities;

public class WhatsAppMessage : Entity
{
    public Guid TenantId { get; private set; }
    public Guid InstanceId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string PhoneNumber { get; private set; }
    public string Body { get; private set; }
    public MessageStatus Status { get; private set; }
    public string EvolutionMessageId { get; private set; }
    public string Error { get; private set; }

    public virtual Tenant Tenant { get; private set; }
    public virtual Instance Instance { get; private set; }
    public virtual Customer Customer { get; private set; }

    public WhatsAppMessage() { }

    public WhatsAppMessage(Guid tenantId, Guid instanceId, Guid customerId, string phoneNumber, string body)
    {
        TenantId = tenantId;
        InstanceId = instanceId;
        CustomerId = customerId;
        PhoneNumber = phoneNumber;
        Body = body;
        Status = MessageStatus.Queued;
        EvolutionMessageId = string.Empty;
        Error = string.Empty;
    }

    public void MarkSent(string evolutionMessageId)
    {
        Status = MessageStatus.Sent;
        EvolutionMessageId = evolutionMessageId;
        SetUpdatedAt();
    }

    public void MarkFailed(string error)
    {
        Status = MessageStatus.Failed;
        Error = error;
        SetUpdatedAt();
    }

    public void MarkDelivered()
    {
        Status = MessageStatus.Delivered;
        SetUpdatedAt();
    }
}
