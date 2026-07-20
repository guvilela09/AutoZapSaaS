using AutoZapSaaS.Domain.Enums;

namespace AutoZapSaaS.Domain.Entities;

/// <summary>
/// Assinatura de um tenant. Todo tenant tem uma: quem não paga fica no Free,
/// para que não exista tenant sem plano — o que deixaria os limites indefinidos.
/// </summary>
public class Subscription : Entity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public PlanTier Tier { get; private set; }
    public SubscriptionStatus Status { get; private set; }

    /// <summary>Id da assinatura no Asaas. Nulo no plano Free, que não é cobrado.</summary>
    public string? AsaasSubscriptionId { get; private set; }
    public string? AsaasCustomerId { get; private set; }

    /// <summary>Até quando o acesso pago vale. Nulo no Free.</summary>
    public DateTime? PeriodoFimEm { get; private set; }

    /// <summary>Início da janela de contagem de mensagens do mês corrente.</summary>
    public DateTime CicloIniciadoEm { get; private set; }
    public int MensagensNoCiclo { get; private set; }

    public virtual Tenant? Tenant { get; private set; }

    private Subscription() { }

    private Subscription(Guid tenantId, PlanTier tier, SubscriptionStatus status)
    {
        TenantId = tenantId;
        Tier = tier;
        Status = status;
        CicloIniciadoEm = DateTime.UtcNow;
        MensagensNoCiclo = 0;
    }

    public static Subscription Gratuita(Guid tenantId) =>
        new(tenantId, PlanTier.Free, SubscriptionStatus.Active);

    public PlanDefinition Plano => PlanCatalog.De(Tier);

    /// <summary>
    /// Se os recursos do plano estão liberados. Overdue continua valendo de propósito:
    /// cortar o WhatsApp de um lojista no primeiro atraso de boleto perde cliente.
    /// </summary>
    public bool EstaAtiva => Status is SubscriptionStatus.Active or SubscriptionStatus.Overdue;

    public void IniciarCobranca(PlanTier tier, string asaasCustomerId, string asaasSubscriptionId)
    {
        Tier = tier;
        AsaasCustomerId = asaasCustomerId;
        AsaasSubscriptionId = asaasSubscriptionId;
        Status = SubscriptionStatus.Pending;
        SetUpdatedAt();
    }

    public void ConfirmarPagamento(DateTime periodoFimEm)
    {
        Status = SubscriptionStatus.Active;
        PeriodoFimEm = periodoFimEm;
        SetUpdatedAt();
    }

    public void MarcarAtrasada()
    {
        Status = SubscriptionStatus.Overdue;
        SetUpdatedAt();
    }

    /// <summary>Suspende e rebaixa para Free: sem plano definido, os limites ficariam soltos.</summary>
    public void Suspender()
    {
        Status = SubscriptionStatus.Suspended;
        Tier = PlanTier.Free;
        SetUpdatedAt();
    }

    public void Cancelar()
    {
        Status = SubscriptionStatus.Canceled;
        Tier = PlanTier.Free;
        AsaasSubscriptionId = null;
        PeriodoFimEm = null;
        SetUpdatedAt();
    }

    /// <summary>
    /// Consome uma mensagem da cota, virando o ciclo se já passou um mês.
    /// Retorna false quando a cota do plano acabou.
    /// </summary>
    public bool TentarConsumirMensagem()
    {
        RenovarCicloSeVencido();

        if (MensagensNoCiclo >= Plano.MaxMensagensPorMes)
            return false;

        MensagensNoCiclo++;
        SetUpdatedAt();
        return true;
    }

    public int MensagensRestantes()
    {
        RenovarCicloSeVencido();
        return Math.Max(0, Plano.MaxMensagensPorMes - MensagensNoCiclo);
    }

    private void RenovarCicloSeVencido()
    {
        if (DateTime.UtcNow < CicloIniciadoEm.AddMonths(1))
            return;

        CicloIniciadoEm = DateTime.UtcNow;
        MensagensNoCiclo = 0;
    }
}
