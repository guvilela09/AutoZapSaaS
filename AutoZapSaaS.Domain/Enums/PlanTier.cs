namespace AutoZapSaaS.Domain.Enums;

public enum PlanTier
{
    Free = 0,
    Pro = 1,
    Business = 2
}

public enum SubscriptionStatus
{
    /// <summary>Assinatura criada, aguardando confirmação do primeiro pagamento.</summary>
    Pending = 0,

    Active = 1,

    /// <summary>Pagamento atrasado. Mantém acesso durante a carência antes de suspender.</summary>
    Overdue = 2,

    /// <summary>Inadimplente após a carência: perde acesso aos recursos pagos.</summary>
    Suspended = 3,

    Canceled = 4
}
