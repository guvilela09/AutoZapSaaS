namespace AutoZapSaaS.Application.DTOs;

public record PlanoResponse(
    string Tier,
    string Nome,
    decimal PrecoMensal,
    int MaxInstancias,
    int MaxMensagensPorMes,
    bool EhAtual);

public record AssinarRequest(string Tier, string FormaPagamento);

public record AssinaturaResponse(
    string Tier,
    string PlanoNome,
    string Status,
    DateTime? PeriodoFimEm,
    int InstanciasUsadas,
    int MaxInstancias,
    int MensagensNoCiclo,
    int MaxMensagensPorMes,
    int MensagensRestantes);
