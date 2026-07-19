namespace AutoZapSaaS.Domain.Entities;

public class SystemUser : Entity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string Role { get; private set; }
    public bool IsActive { get; private set; }
    public virtual Tenant Tenant { get; private set; }

    public SystemUser() { }

    public SystemUser(Guid tenantId, string email, string passwordHash, string role = "Admin")
    {
        TenantId = tenantId;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        IsActive = true;
    }

    public void SetPasswordHash(string hash)
    {
        PasswordHash = hash;
        SetUpdatedAt();
    }

    public void Deactivate()
    {
        IsActive = false;
        SetUpdatedAt();
    }
}
