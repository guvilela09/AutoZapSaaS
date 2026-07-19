namespace AutoZapSaaS.Application.Common.Interfaces;

/// <summary>
/// Tenant da requisição atual. Vive na camada Application para que a Infrastructure
/// possa consumi-lo nos Global Query Filters sem depender da camada de API.
/// </summary>
public interface ITenantContext
{
    Guid TenantId { get; }

    bool IsSet { get; }

    /// <summary>
    /// Define o tenant para requisições que não carregam JWT — hoje, apenas o pipeline
    /// de webhooks, depois de autenticar a origem por token + assinatura HMAC.
    /// Lança se o tenant já veio das claims: um usuário autenticado nunca pode trocar
    /// de tenant no meio da requisição.
    /// </summary>
    void SetTenant(Guid tenantId);
}
