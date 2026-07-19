using AutoZapSaaS.Application.Common.Interfaces;

namespace AutoZapSaaS.API.Common;

/// <summary>
/// Resolve o tenant da requisição a partir da claim "tenant_id" do JWT.
/// Requisições sem JWT (webhooks) nascem sem tenant e precisam chamar
/// <see cref="SetTenant"/> após autenticar a origem.
/// </summary>
public class TenantContext : ITenantContext
{
    private readonly bool _fromClaims;
    private Guid _tenantId;

    public TenantContext(IHttpContextAccessor httpContextAccessor)
    {
        var tenantIdClaim = httpContextAccessor.HttpContext?.User.FindFirst("tenant_id");

        if (Guid.TryParse(tenantIdClaim?.Value, out var tenantId) && tenantId != Guid.Empty)
        {
            _tenantId = tenantId;
            _fromClaims = true;
        }
    }

    public Guid TenantId => _tenantId;

    public bool IsSet => _tenantId != Guid.Empty;

    public void SetTenant(Guid tenantId)
    {
        if (_fromClaims)
            throw new InvalidOperationException(
                "O tenant desta requisição veio do JWT e não pode ser sobrescrito.");

        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId inválido.", nameof(tenantId));

        _tenantId = tenantId;
    }
}
