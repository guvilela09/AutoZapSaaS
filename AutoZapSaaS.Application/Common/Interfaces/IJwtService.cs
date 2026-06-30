namespace AutoZapSaaS.Application.Common.Interfaces;

public interface IJwtService
{
    string GenerateToken(Guid userId, Guid tenantId, string email, string role);
}
