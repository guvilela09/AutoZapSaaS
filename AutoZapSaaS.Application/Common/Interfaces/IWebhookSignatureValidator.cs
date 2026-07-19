using AutoZapSaaS.Domain.Enums;

namespace AutoZapSaaS.Application.Common.Interfaces;

public interface IWebhookSignatureValidator
{
    /// <summary>
    /// Valida a assinatura enviada pela plataforma contra o corpo cru da requisição.
    /// </summary>
    /// <param name="rawBody">Corpo exatamente como recebido — reserializar quebra a assinatura.</param>
    bool IsValid(WebhookPlatform platform, string rawBody, string? signature, string secret);
}
