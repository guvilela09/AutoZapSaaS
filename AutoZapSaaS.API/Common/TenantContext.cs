using System.Security.Claims;

namespace AutoZapSaaS.API.Common;

public interface ITenantContext
{
    Guid TenantId { get; }
}

public class TenantContext : ITenantContext
{
    public Guid TenantId { get; }

    public TenantContext(IHttpContextAccessor httpContextAccessor)
    {
        var userIdClaim = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
        var tenantIdClaim = httpContextAccessor.HttpContext?.User.FindFirst("tenant_id");

        TenantId = Guid.TryParse(tenantIdClaim?.Value ?? string.Empty, out var tenantId)
            ? tenantId
            : Guid.Empty;
    }
}
