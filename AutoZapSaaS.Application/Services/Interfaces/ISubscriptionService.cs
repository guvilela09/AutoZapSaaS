using AutoZapSaaS.Application.DTOs;

namespace AutoZapSaaS.Application.Services.Interfaces;

public interface ISubscriptionService
{
    Task<List<PlanoResponse>> ListarPlanosAsync(Guid tenantId);
    Task<AssinaturaResponse> ObterAtualAsync(Guid tenantId);
    Task<AssinaturaResponse> AssinarAsync(Guid tenantId, AssinarRequest request);
    Task<AssinaturaResponse> CancelarAsync(Guid tenantId);

    /// <summary>
    /// Aplica um evento de cobranca do Asaas. Roda sem tenant na requisicao —
    /// o tenant e descoberto pelo id da assinatura.
    /// </summary>
    Task AplicarEventoDeCobrancaAsync(string evento, string asaasSubscriptionId, DateTime? proximoVencimento);
}
