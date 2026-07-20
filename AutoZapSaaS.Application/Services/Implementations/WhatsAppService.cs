using AutoZapSaaS.Application.Common.Interfaces;
using AutoZapSaaS.Application.DTOs;
using AutoZapSaaS.Application.Mappings;
using AutoZapSaaS.Application.Services.Interfaces;
using AutoZapSaaS.Domain.Entities;
using AutoZapSaaS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutoZapSaaS.Application.Services.Implementations;

public class WhatsAppService : IWhatsAppService
{
    private readonly IApplicationDbContext _context;
    private readonly IEvolutionApiClient _evolutionClient;
    private readonly IPlanLimitService _planLimits;
    private readonly ILogger<WhatsAppService> _logger;

    public WhatsAppService(
        IApplicationDbContext context,
        IEvolutionApiClient evolutionClient,
        IPlanLimitService planLimits,
        ILogger<WhatsAppService> logger)
    {
        _context = context;
        _evolutionClient = evolutionClient;
        _planLimits = planLimits;
        _logger = logger;
    }

    public async Task<WhatsAppMessageResponse> SendMessageAsync(Guid tenantId, SendMessageRequest request)
    {
        var instance = await _context.Instances
            .FirstOrDefaultAsync(i => i.Id == request.InstanceId && i.TenantId == tenantId)
            ?? throw new InvalidOperationException("Instância não encontrada.");

        if (instance.Status != InstanceStatus.Connected)
            throw new InvalidOperationException("Instância não está conectada.");

        // Envio avulso: o telefone pode não corresponder a nenhum cliente cadastrado.
        var customerId = (await _context.Customers
            .FirstOrDefaultAsync(c => c.PhoneNumber == request.PhoneNumber))?.Id;

        await _planLimits.ConsumirMensagemAsync(tenantId);

        var message = new WhatsAppMessage(tenantId, instance.Id, customerId, request.PhoneNumber, request.Message);

        try
        {
            var success = await _evolutionClient.SendMessageAsync(
                instance.SessionName, request.PhoneNumber, request.Message);

            if (success)
                message.MarkSent(Guid.NewGuid().ToString());
            else
                message.MarkFailed("Evolution API retornou falha.");
        }
        catch (Exception ex)
        {
            message.MarkFailed(ex.Message);
            _logger.LogError(ex, "Erro ao enviar mensagem para {Phone}", request.PhoneNumber);
        }

        _context.WhatsAppMessages.Add(message);
        await _context.SaveChangesAsync(CancellationToken.None);

        return message.ParaResposta();
    }

    public async Task<WhatsAppMessageResponse> SendTemplateAsync(Guid tenantId, SendTemplateMessageRequest request)
    {
        var instance = await _context.Instances
            .FirstOrDefaultAsync(i => i.Id == request.InstanceId && i.TenantId == tenantId)
            ?? throw new InvalidOperationException("Instância não encontrada.");

        if (instance.Status != InstanceStatus.Connected)
            throw new InvalidOperationException("Instância não está conectada.");

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId && c.TenantId == tenantId)
            ?? throw new InvalidOperationException("Cliente não encontrado.");

        var template = await _context.MessageTemplates
            .FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.TenantId == tenantId)
            ?? throw new InvalidOperationException("Template não encontrado.");

        var body = template.Render(customer.Name, $"Mensagem automática");
        await _planLimits.ConsumirMensagemAsync(tenantId);

        var message = new WhatsAppMessage(tenantId, instance.Id, customer.Id, customer.PhoneNumber, body);

        try
        {
            var success = await _evolutionClient.SendMessageAsync(
                instance.SessionName, customer.PhoneNumber, body);

            if (success)
                message.MarkSent(Guid.NewGuid().ToString());
            else
                message.MarkFailed("Evolution API retornou falha.");
        }
        catch (Exception ex)
        {
            message.MarkFailed(ex.Message);
            _logger.LogError(ex, "Erro ao enviar template para {Phone}", customer.PhoneNumber);
        }

        _context.WhatsAppMessages.Add(message);
        await _context.SaveChangesAsync(CancellationToken.None);

        return message.ParaResposta();
    }

    public async Task<List<WhatsAppMessageResponse>> GetMessagesAsync(Guid tenantId)
    {
        var messages = await _context.WhatsAppMessages
            .Include(m => m.Customer)
            .Where(m => m.TenantId == tenantId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(200)
            .ToListAsync();

        return messages.ParaRespostas(e => e.ParaResposta());
    }

    public async Task ProcessPendingMessagesAsync()
    {
        var pending = await _context.WhatsAppMessages
            .Include(m => m.Instance)
            .Where(m => m.Status == MessageStatus.Queued)
            .ToListAsync();

        foreach (var msg in pending)
        {
            // O numero que originou a mensagem pode ter sido removido enquanto ela
            // esperava na fila. Sem instancia nao ha por onde enviar.
            if (msg.Instance is null)
            {
                msg.MarkFailed("O número de WhatsApp usado por esta mensagem foi removido.");
                _context.WhatsAppMessages.Update(msg);
                continue;
            }

            try
            {
                var success = await _evolutionClient.SendMessageAsync(
                    msg.Instance.SessionName, msg.PhoneNumber, msg.Body);

                if (success)
                    msg.MarkSent(Guid.NewGuid().ToString());
                else
                    msg.MarkFailed("Falha no reprocessamento.");
            }
            catch (Exception ex)
            {
                msg.MarkFailed(ex.Message);
            }

            _context.WhatsAppMessages.Update(msg);
        }

        await _context.SaveChangesAsync(CancellationToken.None);
    }
}
