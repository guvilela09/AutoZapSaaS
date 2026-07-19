using AutoMapper;
using AutoZapSaaS.Application.Common.Interfaces;
using AutoZapSaaS.Application.DTOs;
using AutoZapSaaS.Application.Services.Interfaces;
using AutoZapSaaS.Domain.Entities;
using AutoZapSaaS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutoZapSaaS.Application.Services.Implementations;

public class MessageTemplateService : IMessageTemplateService
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<MessageTemplateService> _logger;

    public MessageTemplateService(IApplicationDbContext context, IMapper mapper, ILogger<MessageTemplateService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<MessageTemplateResponse> CreateAsync(Guid tenantId, CreateMessageTemplateRequest request)
    {
        if (!Enum.TryParse<EventType>(request.EventType, true, out var eventType))
            throw new ArgumentException($"Tipo de evento inválido: {request.EventType}");

        var template = new MessageTemplate(tenantId, request.Name, eventType, request.Body);
        _context.MessageTemplates.Add(template);
        await _context.SaveChangesAsync(CancellationToken.None);

        _logger.LogInformation("Template criado: {TemplateId} - {Name}", template.Id, template.Name);
        return _mapper.Map<MessageTemplateResponse>(template);
    }

    public async Task<MessageTemplateResponse?> GetByIdAsync(Guid id)
    {
        var template = await _context.MessageTemplates.FirstOrDefaultAsync(t => t.Id == id);
        return template is null ? null : _mapper.Map<MessageTemplateResponse>(template);
    }

    public async Task<List<MessageTemplateResponse>> GetByTenantAsync(Guid tenantId)
    {
        var templates = await _context.MessageTemplates
            .Where(t => t.TenantId == tenantId)
            .OrderBy(t => t.Name)
            .ToListAsync();

        return _mapper.Map<List<MessageTemplateResponse>>(templates);
    }

    public async Task<MessageTemplateResponse?> UpdateAsync(Guid id, UpdateMessageTemplateRequest request)
    {
        var template = await _context.MessageTemplates.FirstOrDefaultAsync(t => t.Id == id);
        if (template is null) return null;

        if (!Enum.TryParse<EventType>(request.EventType, true, out var eventType))
            throw new ArgumentException($"Tipo de evento inválido: {request.EventType}");

        typeof(MessageTemplate).GetProperty(nameof(MessageTemplate.Name))!.SetValue(template, request.Name);
        typeof(MessageTemplate).GetProperty(nameof(MessageTemplate.Body))!.SetValue(template, request.Body);
        template.SetUpdatedAt();

        await _context.SaveChangesAsync(CancellationToken.None);
        return _mapper.Map<MessageTemplateResponse>(template);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var template = await _context.MessageTemplates.FirstOrDefaultAsync(t => t.Id == id);
        if (template is null) return false;

        _context.MessageTemplates.Remove(template);
        await _context.SaveChangesAsync(CancellationToken.None);
        return true;
    }
}
