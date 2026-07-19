using AutoZapSaaS.Application.Common;
using AutoZapSaaS.Application.Common.Interfaces;
using AutoZapSaaS.Application.DTOs;
using AutoZapSaaS.Application.Services.Interfaces;
using AutoZapSaaS.Domain.Entities;
using AutoZapSaaS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutoZapSaaS.Application.Services.Implementations;

public class SubscriptionService : ISubscriptionService
{
    private readonly IApplicationDbContext _context;
    private readonly IAsaasClient _asaas;
    private readonly IPlanLimitService _planLimits;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(
        IApplicationDbContext context,
        IAsaasClient asaas,
        IPlanLimitService planLimits,
        ILogger<SubscriptionService> logger)
    {
        _context = context;
        _asaas = asaas;
        _planLimits = planLimits;
        _logger = logger;
    }

    public async Task<List<PlanoResponse>> ListarPlanosAsync(Guid tenantId)
    {
        var assinatura = await _planLimits.ObterOuCriarAsync(tenantId);

        return PlanCatalog.Todos
            .Select(p => new PlanoResponse(
                p.Tier.ToString(), p.Nome, p.PrecoMensal,
                p.MaxInstancias, p.MaxMensagensPorMes,
                EhAtual: p.Tier == assinatura.Tier))
            .ToList();
    }

    public async Task<AssinaturaResponse> ObterAtualAsync(Guid tenantId)
    {
        var assinatura = await _planLimits.ObterOuCriarAsync(tenantId);
        return await MontarRespostaAsync(assinatura);
    }

    public async Task<AssinaturaResponse> AssinarAsync(Guid tenantId, AssinarRequest request)
    {
        if (!Enum.TryParse<PlanTier>(request.Tier, true, out var tier))
            throw new ArgumentException($"Plano invalido: {request.Tier}");

        if (tier == PlanTier.Free)
            throw new ArgumentException("Para voltar ao Free, cancele a assinatura atual.");

        if (!Enum.TryParse<AsaasFormaPagamento>(request.FormaPagamento, true, out var forma))
            forma = AsaasFormaPagamento.Indefinida;

        var assinatura = await _planLimits.ObterOuCriarAsync(tenantId);

        if (assinatura.Tier == tier && assinatura.EstaAtiva)
            throw new InvalidOperationException($"Voce ja esta no plano {PlanCatalog.De(tier).Nome}.");

        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId)
            ?? throw new InvalidOperationException("Tenant nao encontrado.");

        var plano = PlanCatalog.De(tier);

        // Cliente no Asaas e reaproveitado entre trocas de plano.
        var customerId = assinatura.AsaasCustomerId;
        if (string.IsNullOrWhiteSpace(customerId))
        {
            var cliente = await _asaas.CriarOuAtualizarClienteAsync(
                tenant.Name, tenant.Email, tenant.Document);
            customerId = cliente.Id;
        }

        // Troca de plano: cancela a anterior antes de abrir outra, senao o lojista
        // ficaria com duas cobrancas ativas.
        if (!string.IsNullOrWhiteSpace(assinatura.AsaasSubscriptionId))
        {
            await _asaas.CancelarAssinaturaAsync(assinatura.AsaasSubscriptionId);
        }

        var nova = await _asaas.CriarAssinaturaAsync(
            customerId, plano.PrecoMensal, $"AutoZap - plano {plano.Nome}", forma);

        assinatura.IniciarCobranca(tier, customerId, nova.Id);
        await _context.SaveChangesAsync(CancellationToken.None);

        _logger.LogInformation(
            "Assinatura {AsaasId} criada para o tenant {TenantId} no plano {Plano}",
            nova.Id, tenantId, plano.Nome);

        return await MontarRespostaAsync(assinatura);
    }

    public async Task<AssinaturaResponse> CancelarAsync(Guid tenantId)
    {
        var assinatura = await _planLimits.ObterOuCriarAsync(tenantId);

        if (!string.IsNullOrWhiteSpace(assinatura.AsaasSubscriptionId))
            await _asaas.CancelarAssinaturaAsync(assinatura.AsaasSubscriptionId);

        assinatura.Cancelar();
        await _context.SaveChangesAsync(CancellationToken.None);

        _logger.LogInformation("Assinatura do tenant {TenantId} cancelada", tenantId);
        return await MontarRespostaAsync(assinatura);
    }

    public async Task AplicarEventoDeCobrancaAsync(
        string evento, string asaasSubscriptionId, DateTime? proximoVencimento)
    {
        // Sem tenant na requisicao: o webhook do Asaas e publico e o tenant e
        // descoberto pelo id da assinatura.
        var assinatura = await _context.Subscriptions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.AsaasSubscriptionId == asaasSubscriptionId);

        if (assinatura is null)
        {
            _logger.LogWarning(
                "Evento {Evento} do Asaas para assinatura desconhecida {AsaasId}",
                evento, asaasSubscriptionId);
            return;
        }

        switch (evento)
        {
            case "PAYMENT_CONFIRMED":
            case "PAYMENT_RECEIVED":
                assinatura.ConfirmarPagamento(proximoVencimento ?? DateTime.UtcNow.AddMonths(1));
                break;

            case "PAYMENT_OVERDUE":
                // Mantem o acesso: cortar no primeiro atraso de boleto perde cliente.
                assinatura.MarcarAtrasada();
                break;

            case "PAYMENT_DELETED":
            case "SUBSCRIPTION_DELETED":
                assinatura.Cancelar();
                break;

            default:
                _logger.LogInformation("Evento {Evento} do Asaas ignorado", evento);
                return;
        }

        await _context.SaveChangesAsync(CancellationToken.None);

        _logger.LogInformation(
            "Evento {Evento} aplicado a assinatura do tenant {TenantId}: status {Status}",
            evento, assinatura.TenantId, assinatura.Status);
    }

    private async Task<AssinaturaResponse> MontarRespostaAsync(Subscription assinatura)
    {
        var plano = assinatura.Plano;
        var instancias = await _context.Instances.IgnoreQueryFilters()
            .CountAsync(i => i.TenantId == assinatura.TenantId);

        return new AssinaturaResponse(
            assinatura.Tier.ToString(),
            plano.Nome,
            assinatura.Status.ToString(),
            assinatura.PeriodoFimEm,
            instancias,
            plano.MaxInstancias,
            plano.MaxMensagensPorMes - assinatura.MensagensRestantes(),
            plano.MaxMensagensPorMes,
            assinatura.MensagensRestantes());
    }
}
