namespace AutoZapSaaS.Application.DTOs;

public record CreateMessageTemplateRequest(string Name, string EventType, string Body);

public record UpdateMessageTemplateRequest(string Name, string EventType, string Body);

public record MessageTemplateResponse(
    Guid Id,
    Guid TenantId,
    string Name,
    string EventType,
    string Body,
    bool IsActive,
    DateTime CreatedAt);

public record WhatsAppMessageResponse(
    Guid Id,
    string PhoneNumber,
    string Body,
    string Status,
    string CustomerName,
    DateTime CreatedAt);
