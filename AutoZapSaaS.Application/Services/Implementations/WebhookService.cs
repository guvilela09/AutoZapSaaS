using System.Text.Json;
using AutoZapSaaS.Application.Common;
using AutoZapSaaS.Application.Common.Interfaces;
using AutoZapSaaS.Application.DTOs;
using AutoZapSaaS.Application.Mappings;
using AutoZapSaaS.Application.Services.Interfaces;
using AutoZapSaaS.Domain.Entities;
using AutoZapSaaS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutoZapSaaS.Application.Services.Implementations;

public class WebhookService : IWebhookService
{
    private readonly IApplicationDbContext _context;
    private readonly IEvolutionApiClient _evolutionClient;
    private readonly IPlanLimitService _planLimits;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(
        IApplicationDbContext context,
        IEvolutionApiClient evolutionClient,
        IPlanLimitService planLimits,
        ILogger<WebhookService> logger)
    {
        _context = context;
        _evolutionClient = evolutionClient;
        _planLimits = planLimits;
        _logger = logger;
    }

    public async Task<WebhookEventResponse> ProcessKiwifyAsync(Guid tenantId, JsonElement payload)
    {
        var rawPayload = payload.GetRawText();

        // Reentrega da plataforma: devolve o resultado anterior sem reenviar nada.
        var jaProcessado = await BuscarEntregaAnteriorAsync(tenantId, rawPayload);
        if (jaProcessado is not null)
        {
            _logger.LogInformation(
                "Webhook Kiwify reentregue para o tenant {TenantId}; ignorado", tenantId);
            return jaProcessado.ParaResposta();
        }

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

        return webhookEvent.ParaResposta();
    }

    public async Task<WebhookEventResponse> ProcessHotmartAsync(Guid tenantId, JsonElement payload)
    {
        var rawPayload = payload.GetRawText();

        // Reentrega da plataforma: devolve o resultado anterior sem reenviar nada.
        var jaProcessado = await BuscarEntregaAnteriorAsync(tenantId, rawPayload);
        if (jaProcessado is not null)
        {
            _logger.LogInformation(
                "Webhook Hotmart reentregue para o tenant {TenantId}; ignorado", tenantId);
            return jaProcessado.ParaResposta();
        }

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

        return webhookEvent.ParaResposta();
    }

    public async Task<WebhookEventResponse> ProcessNuvemshopAsync(Guid tenantId, JsonElement payload)
    {
        var rawPayload = payload.GetRawText();

        // Reentrega da plataforma: devolve o resultado anterior sem reenviar nada.
        var jaProcessado = await BuscarEntregaAnteriorAsync(tenantId, rawPayload);
        if (jaProcessado is not null)
        {
            _logger.LogInformation(
                "Webhook Nuvemshop reentregue para o tenant {TenantId}; ignorado", tenantId);
            return jaProcessado.ParaResposta();
        }

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

        return webhookEvent.ParaResposta();
    }

    /// <summary>
    /// Reconhece reentrega do mesmo webhook. As plataformas reenviam em timeout ou
    /// erro, e reprocessar dispararia a mesma mensagem de novo para o consumidor
    /// final — alem de consumir a cota duas vezes.
    /// A janela de 24h evita bloquear um pedido legitimamente identico dias depois.
    /// </summary>
    private async Task<WebhookEvent?> BuscarEntregaAnteriorAsync(Guid tenantId, string rawPayload)
    {
        var hash = WebhookEvent.CalcularHash(rawPayload);
        var limite = DateTime.UtcNow.AddHours(-24);

        return await _context.WebhookEvents
            .Where(w => w.TenantId == tenantId
                        && w.PayloadHash == hash
                        && w.Processed
                        && w.CreatedAt >= limite)
            .FirstOrDefaultAsync();
    }

    private async Task UpsertCustomerAndNotifyAsync(
        Guid tenantId, string name, string phone, string email,
        string origin, WebhookEvent webhookEvent)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.PhoneNumber == phone);

        if (customer is not null)
        {
            customer.Name = name;
            customer.SetUpdatedAt();
            _logger.LogInformation("Cliente atualizado via webhook: {Phone}", phone);
        }
        else
        {
            customer = new Customer(tenantId, name, phone, email, origin);
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

            var message = new WhatsAppMessage(tenantId, instance.Id, customer.Id, phone, body);

            try
            {
                // Cota estourada não pode virar erro HTTP: a plataforma de vendas
                // reenviaria o webhook indefinidamente. Registra como falha e sai.
                await _planLimits.ConsumirMensagemAsync(tenantId);
            }
            catch (PlanLimitException ex)
            {
                message.MarkFailed(ex.Message);
                _context.WhatsAppMessages.Add(message);
                await _context.SaveChangesAsync(CancellationToken.None);

                _logger.LogWarning(
                    "Mensagem não enviada para {Phone}: cota do plano do tenant {TenantId} esgotada",
                    phone, tenantId);
                return;
            }

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
