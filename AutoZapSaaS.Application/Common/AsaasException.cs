namespace AutoZapSaaS.Application.Common;

/// <summary>
/// Falha ao falar com o Asaas. Separada de EvolutionApiException para que a API
/// distinga "o WhatsApp caiu" de "a cobranca caiu" na resposta e nos alertas.
/// </summary>
public class AsaasException : Exception
{
    public AsaasException(string message, Exception? inner = null) : base(message, inner) { }
}
