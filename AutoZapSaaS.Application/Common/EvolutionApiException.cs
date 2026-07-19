using System.Net;

namespace AutoZapSaaS.Application.Common;

/// <summary>
/// Falha ao falar com a Evolution API. Existe para que a camada de API responda
/// 502 (falha em serviço externo) em vez de 500 genérico, e para carregar o corpo
/// da resposta — é lá que a Evolution explica o motivo real.
/// </summary>
public class EvolutionApiException : Exception
{
    public HttpStatusCode? StatusCode { get; }
    public string? ResponseBody { get; }

    public EvolutionApiException(string message, HttpStatusCode? statusCode = null, string? responseBody = null, Exception? inner = null)
        : base(message, inner)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}
