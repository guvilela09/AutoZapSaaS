namespace AutoZapSaaS.Domain.Entities;

/// <summary>
/// Marca uma entidade como pertencente a um tenant. Toda entidade que implementa
/// esta interface recebe automaticamente um Global Query Filter no ApplicationDbContext,
/// garantindo que nenhuma consulta atravesse a fronteira de um tenant.
/// </summary>
public interface ITenantEntity
{
    Guid TenantId { get; }
}
