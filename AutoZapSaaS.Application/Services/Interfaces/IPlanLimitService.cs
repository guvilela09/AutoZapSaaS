using AutoZapSaaS.Domain.Entities;

namespace AutoZapSaaS.Application.Services.Interfaces;

public interface IPlanLimitService
{
    /// <summary>Assinatura do tenant, criando uma Free se ainda não existir.</summary>
    Task<Subscription> ObterOuCriarAsync(Guid tenantId);

    /// <summary>Lança <see cref="Common.PlanLimitException"/> se o plano não permitir mais instâncias.</summary>
    Task GarantirPodeCriarInstanciaAsync(Guid tenantId);

    /// <summary>
    /// Consome uma mensagem da cota mensal. Lança se a cota acabou.
    /// Nao persiste — quem chama salva junto com a mensagem, na mesma transacao.
    /// </summary>
    Task ConsumirMensagemAsync(Guid tenantId);
}
