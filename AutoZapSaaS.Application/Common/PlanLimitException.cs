namespace AutoZapSaaS.Application.Common;

/// <summary>
/// Limite do plano atingido. A API responde 402 Payment Required — é uma restrição
/// comercial, não um erro do pedido nem uma falha nossa.
/// </summary>
public class PlanLimitException : Exception
{
    public string PlanoAtual { get; }
    public string? PlanoSugerido { get; }

    public PlanLimitException(string message, string planoAtual, string? planoSugerido = null)
        : base(message)
    {
        PlanoAtual = planoAtual;
        PlanoSugerido = planoSugerido;
    }
}
