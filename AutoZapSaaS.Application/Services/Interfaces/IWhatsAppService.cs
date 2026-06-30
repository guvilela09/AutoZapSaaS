using AutoZapSaaS.Application.DTOs;

namespace AutoZapSaaS.Application.Services.Interfaces;

public interface IWhatsAppService
{
    Task<WhatsAppMessageResponse> SendMessageAsync(Guid tenantId, SendMessageRequest request);
    Task<WhatsAppMessageResponse> SendTemplateAsync(Guid tenantId, SendTemplateMessageRequest request);
    Task<List<WhatsAppMessageResponse>> GetMessagesAsync(Guid tenantId);
    Task ProcessPendingMessagesAsync();
}
