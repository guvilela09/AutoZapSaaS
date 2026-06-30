using AutoZapSaaS.Application.DTOs;

namespace AutoZapSaaS.Application.Services.Interfaces;

public interface IMessageTemplateService
{
    Task<MessageTemplateResponse> CreateAsync(Guid tenantId, CreateMessageTemplateRequest request);
    Task<MessageTemplateResponse?> GetByIdAsync(Guid id);
    Task<List<MessageTemplateResponse>> GetByTenantAsync(Guid tenantId);
    Task<MessageTemplateResponse?> UpdateAsync(Guid id, UpdateMessageTemplateRequest request);
    Task<bool> DeleteAsync(Guid id);
}
