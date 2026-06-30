using System.Text.Json;
using AutoMapper;
using AutoZapSaaS.Application.Common.Interfaces;
using AutoZapSaaS.Application.DTOs;
using AutoZapSaaS.Application.Services.Interfaces;
using AutoZapSaaS.Domain.Entities;
using AutoZapSaaS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutoZapSaaS.Application.Services.Implementations;

public class WebhookService : IWebhookService
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IEvolutionApiClient _evolutionClient;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(
        IApplicationDbContext context,
        IMapper mapper,
        IEvolutionApiClient evolutionClient,
        ILogger<WebhookService> logger)
    {
        _context = context;
        _mapper = mapper;
        _evolutionClient = evolutionClient;
        _logger = logger;
    }

    public async Task<WebhookEventResponse> ProcessKiwifyAsync(Guid tenantId, JsonElement payload)
    {
        var rawPayload = payload.GetRawText();
        var webhookEvent = new WebhookEvent(tenantId, WebhookPlatform.Kiwify, EventType.PaymentConfirmed, rawPayload);

        try
        {
            var name = payload.TryGetProperty("customer", out var c) && c.TryGetProperty("name", out var n)
                ? n.GetString() : null;
            var phone = payload.TryGetProperty("customer", out var c2) && c2.TryGetProperty("phone", out var p)
                ? p.GetString() : null;
            var email = payload.TryGetProperty("customer", out var c3) && c3.TryGetProperty("email", out var e)
                ? e.GetString() : "sem-email@kiwify.com";

            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(phone))
            {
                await UpsertCustomerAndNotifyAsync(tenantId, name, phone, email ?? "sem-email@kiwify.com", "Kiwify", webhookEvent);
            }

            webhookEvent.MarkProcessed();
        }
        catch (Exception ex)
        {
            webhookEvent.MarkFailed(ex.Message);
            _logger.LogError(ex, "Erro ao processar webhook Kiwify");
        }

        _context.WebhookEvents.Add(webhookEvent);
        await _context.SaveChangesAsync(CancellationToken.None);

        return _mapper.Map<WebhookEventResponse>(webhookEvent);
    }

    public async Task<WebhookEventResponse> ProcessHotmartAsync(Guid tenantId, JsonElement payload)
    {
        var rawPayload = payload.GetRawText();
        var webhookEvent = new WebhookEvent(tenantId, WebhookPlatform.Hotmart, EventType.PaymentConfirmed, rawPayload);

        try
        {
            var data = payload.TryGetProperty("data", out var d) ? d : payload;
            var name = data.TryGetProperty("buyer_name", out var n) || data.TryGetProperty("BuyerName", out n)
                ? n.GetString() : null;
            var phone = data.TryGetProperty("buyer_phone", out var p) || data.TryGetProperty("BuyerPhone", out p)
                ? p.GetString() : null;
            var email = data.TryGetProperty("buyer_email", out var e) || data.TryGetProperty("BuyerEmail", out e)
                ? e.GetString() : "sem-email@hotmart.com";

            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(phone))
            {
                await UpsertCustomerAndNotifyAsync(tenantId, name, phone, email ?? "sem-email@hotmart.com", "Hotmart", webhookEvent);
            }

            webhookEvent.MarkProcessed();
        }
        catch (Exception ex)
        {
            webhookEvent.MarkFailed(ex.Message);
            _logger.LogError(ex, "Erro ao processar webhook Hotmart");
        }

        _context.WebhookEvents.Add(webhookEvent);
        await _context.SaveChangesAsync(CancellationToken.None);

        return _mapper.Map<WebhookEventResponse>(webhookEvent);
    }

    public async Task<WebhookEventResponse> ProcessNuvemshopAsync(Guid tenantId, JsonElement payload)
    {
        var rawPayload = payload.GetRawText();
        var webhookEvent = new WebhookEvent(tenantId, WebhookPlatform.Nuvemshop, EventType.OrderCreated, rawPayload);

        try
        {
            var name = payload.TryGetProperty("customer_name", out var n) || payload.TryGetProperty("CustomerName", out n)
                ? n.GetString() : null;
            var phone = payload.TryGetProperty("customer_phone", out var p) || payload.TryGetProperty("CustomerPhone", out p)
                ? p.GetString() : null;
            var email = payload.TryGetProperty("customer_email", out var e) || payload.TryGetProperty("CustomerEmail", out e)
                ? e.GetString() : "sem-email@nuvemshop.com";

            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(phone))
            {
                await UpsertCustomerAndNotifyAsync(tenantId, name, phone, email ?? "sem-email@nuvemshop.com", "Nuvemshop", webhookEvent);
            }

            webhookEvent.MarkProcessed();
        }
        catch (Exception ex)
        {
            webhookEvent.MarkFailed(ex.Message);
            _logger.LogError(ex, "Erro ao processar webhook Nuvemshop");
        }

        _context.WebhookEvents.Add(webhookEvent);
        await _context.SaveChangesAsync(CancellationToken.None);

        return _mapper.Map<WebhookEventResponse>(webhookEvent);
    }

    private async Task UpsertCustomerAndNotifyAsync(
        Guid tenantId, string name, string phone, string email,
        string origin, WebhookEvent webhookEvent)
    {
        var existingCustomer = await _context.Customers
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.PhoneNumber == phone);

        if (existingCustomer is not null)
        {
            existingCustomer.UpdatePhoneNumber(phone);
            typeof(Customer).GetProperty(nameof(Customer.Name))!.SetValue(existingCustomer, name);
            _logger.LogInformation("Cliente atualizado via webhook: {Phone}", phone);
        }
        else
        {
            var customer = new Customer(tenantId, name, phone, email, origin);
            _context.Customers.Add(customer);
            _logger.LogInformation("Novo cliente via webhook: {Name} - {Phone}", name, phone);
        }

        await _context.SaveChangesAsync(CancellationToken.None);

        var instance = await _context.Instances
            .Where(i => i.TenantId == tenantId && i.Status == InstanceStatus.Connected)
            .FirstOrDefaultAsync();

        if (instance is not null)
        {
            var template = await _context.MessageTemplates
                .Where(t => t.TenantId == tenantId && t.IsActive && t.EventType == webhookEvent.EventType)
                .FirstOrDefaultAsync();

            var body = template is not null
                ? template.Render(name, $"Pedido confirmado via {origin}")
                : $"Olá {name}! Seu pedido na {origin} foi confirmado com sucesso. Qualquer dúvida, é só responder por aqui!";

            var message = new WhatsAppMessage(tenantId, instance.Id, Guid.Empty, phone, body);

            try
            {
                var success = await _evolutionClient.SendMessageAsync(instance.SessionName, phone, body);
                if (success)
                    message.MarkSent(Guid.NewGuid().ToString());
                else
                    message.MarkFailed("Falha no envio via Evolution API");
            }
            catch (Exception ex)
            {
                message.MarkFailed(ex.Message);
                _logger.LogError(ex, "Erro ao enviar WhatsApp para {Phone}", phone);
            }

            _context.WhatsAppMessages.Add(message);
            await _context.SaveChangesAsync(CancellationToken.None);
        }
    }
}
