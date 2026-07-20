using AutoZapSaaS.Application.Common;
using AutoZapSaaS.Application.Common.Interfaces;
using AutoZapSaaS.Application.Services.Interfaces;
using AutoZapSaaS.Domain.Entities;
using AutoZapSaaS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutoZapSaaS.Application.Services.Implementations;

public class PlanLimitService : IPlanLimitService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<PlanLimitService> _logger;

    public PlanLimitService(IApplicationDbContext context, ILogger<PlanLimitService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Subscription> ObterOuCriarAsync(Guid tenantId)
    {
        var assinatura = await _context.Subscriptions.FirstOrDefaultAsync(s => s.TenantId == tenantId);
        if (assinatura is not null)
            return assinatura;

        // Rede de segurança: tenant sem assinatura (criado antes da cobrança existir,
        // ou por uma falha no cadastro) cai no Free em vez de ficar sem limites.
        _logger.LogWarning("Tenant {TenantId} sem assinatura. Criando Free.", tenantId);

        assinatura = Subscription.Gratuita(tenantId);
        _context.Subscriptions.Add(assinatura);
        await _context.SaveChangesAsync(CancellationToken.None);

        return assinatura;
    }

    public async Task GarantirPodeCriarInstanciaAsync(Guid tenantId)
    {
        var assinatura = await ObterOuCriarAsync(tenantId);
        var plano = assinatura.Plano;

        var instanciasAtuais = await _context.Instances.CountAsync();

        if (instanciasAtuais >= plano.MaxInstancias)
        {
            throw new PlanLimitException(
                $"O plano {plano.Nome} permite {plano.MaxInstancias} " +
                $"instância(s) de WhatsApp. Faça upgrade para conectar mais números.",
                plano.Nome,
                SugerirUpgrade(plano.Tier));
        }
    }

    public async Task ConsumirMensagemAsync(Guid tenantId)
    {
        var assinatura = await ObterOuCriarAsync(tenantId);

        if (!assinatura.TentarConsumirMensagem())
        {
            var plano = assinatura.Plano;

            throw new PlanLimitException(
                $"A cota de {plano.MaxMensagensPorMes} mensagens/mês do plano " +
                $"{plano.Nome} acabou. Faça upgrade para continuar enviando.",
                plano.Nome,
                SugerirUpgrade(plano.Tier));
        }
    }

    private static string? SugerirUpgrade(PlanTier atual) => atual switch
    {
        PlanTier.Free => PlanCatalog.Pro.Nome,
        PlanTier.Pro => PlanCatalog.Business.Nome,
        _ => null
    };
}
