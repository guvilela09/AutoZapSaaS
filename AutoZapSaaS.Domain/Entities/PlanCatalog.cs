using AutoZapSaaS.Domain.Enums;

namespace AutoZapSaaS.Domain.Entities;

/// <summary>
/// Limites de cada plano. Fica em código, e não em tabela, porque plano é regra de
/// negócio versionada junto com o produto — e assim não existe o risco de um banco
/// sem seed liberar acesso ilimitado.
/// </summary>
public record PlanDefinition(
    PlanTier Tier,
    string Nome,
    decimal PrecoMensal,
    int MaxInstancias,
    int MaxMensagensPorMes)
{
    public bool EhGratuito => PrecoMensal == 0m;
}

public static class PlanCatalog
{
    public static readonly PlanDefinition Free = new(
        PlanTier.Free, "Free", 0m, MaxInstancias: 1, MaxMensagensPorMes: 100);

    public static readonly PlanDefinition Pro = new(
        PlanTier.Pro, "Pro", 97m, MaxInstancias: 3, MaxMensagensPorMes: 5_000);

    public static readonly PlanDefinition Business = new(
        PlanTier.Business, "Business", 297m, MaxInstancias: 10, MaxMensagensPorMes: 50_000);

    public static IReadOnlyList<PlanDefinition> Todos { get; } = new[] { Free, Pro, Business };

    public static PlanDefinition De(PlanTier tier) =>
        Todos.FirstOrDefault(p => p.Tier == tier)
        ?? throw new ArgumentOutOfRangeException(nameof(tier), $"Plano desconhecido: {tier}");
}
