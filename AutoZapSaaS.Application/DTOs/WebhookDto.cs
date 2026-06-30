namespace AutoZapSaaS.Application.DTOs;

public record KiwifyWebhookPayload(CustomerWebhookData Customer, string Event);

public record HotmartWebhookPayload(HotmartData Data, string Event);

public record NuvemshopWebhookPayload(NuvemshopData Data, string Event);

public record CustomerWebhookData(string Name, string Phone, string Email);

public record HotmartData(
    string BuyerName,
    string BuyerPhone,
    string BuyerEmail,
    string ProductName,
    string Status);

public record NuvemshopData(
    string CustomerName,
    string CustomerPhone,
    string CustomerEmail,
    decimal Total,
    string OrderNumber);

public record WebhookEventResponse(
    Guid Id,
    string Platform,
    string EventType,
    bool Processed,
    DateTime CreatedAt);

public record SendMessageRequest(
    Guid InstanceId,
    string PhoneNumber,
    string Message);

public record SendTemplateMessageRequest(
    Guid InstanceId,
    Guid CustomerId,
    Guid TemplateId);
