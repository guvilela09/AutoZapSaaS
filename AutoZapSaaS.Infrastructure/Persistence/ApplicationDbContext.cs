using AutoZapSaaS.Application.Common.Interfaces;
using AutoZapSaaS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoZapSaaS.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ITenantContext _tenantContext;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Instance> Instances => Set<Instance>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<SystemUser> SystemUsers => Set<SystemUser>();
    public DbSet<WebhookEvent> WebhookEvents => Set<WebhookEvent>();
    public DbSet<MessageTemplate> MessageTemplates => Set<MessageTemplate>();
    public DbSet<WhatsAppMessage> WhatsAppMessages => Set<WhatsAppMessage>();
    public DbSet<WebhookIntegration> WebhookIntegrations => Set<WebhookIntegration>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Tenant>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Name).IsRequired().HasMaxLength(100);
            e.Property(t => t.Document).HasMaxLength(20);

            e.HasMany(t => t.Instances)
             .WithOne(i => i.Tenant)
             .HasForeignKey(i => i.TenantId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(t => t.Customers)
             .WithOne(c => c.Tenant)
             .HasForeignKey(c => c.TenantId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Instance>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.Name).IsRequired().HasMaxLength(50);
            e.Property(i => i.Token).IsRequired();
        });

        modelBuilder.Entity<Customer>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.PhoneNumber).IsRequired().HasMaxLength(20);
            e.HasIndex(c => new { c.TenantId, c.PhoneNumber });
        });

        modelBuilder.Entity<SystemUser>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Email).IsRequired().HasMaxLength(200);
            e.HasIndex(u => u.Email).IsUnique();
            e.HasOne(u => u.Tenant)
             .WithMany()
             .HasForeignKey(u => u.TenantId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WebhookEvent>(e =>
        {
            e.HasKey(w => w.Id);
            e.Property(w => w.RawPayload).HasColumnType("nvarchar(max)");
            e.Property(w => w.Error).HasMaxLength(500);
        });

        modelBuilder.Entity<MessageTemplate>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Name).IsRequired().HasMaxLength(100);
            e.Property(t => t.Body).IsRequired().HasColumnType("nvarchar(max)");
            e.HasOne(t => t.Tenant)
             .WithMany()
             .HasForeignKey(t => t.TenantId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WhatsAppMessage>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.PhoneNumber).IsRequired().HasMaxLength(20);
            e.Property(m => m.Body).IsRequired().HasColumnType("nvarchar(max)");
            e.Property(m => m.EvolutionMessageId).HasMaxLength(100);
            e.Property(m => m.Error).HasMaxLength(500);

            e.HasOne(m => m.Tenant)
             .WithMany()
             .HasForeignKey(m => m.TenantId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(m => m.Instance)
             .WithMany()
             .HasForeignKey(m => m.InstanceId)
             .OnDelete(DeleteBehavior.Restrict);

            // Apagar um cliente não pode apagar nem travar o histórico de mensagens:
            // a mensagem sobrevive, apenas perde o vínculo.
            e.HasOne(m => m.Customer)
             .WithMany()
             .HasForeignKey(m => m.CustomerId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<WebhookIntegration>(e =>
        {
            e.HasKey(w => w.Id);
            e.Property(w => w.Token).IsRequired().HasMaxLength(64);
            e.Property(w => w.Secret).IsRequired().HasMaxLength(200);
            e.HasIndex(w => w.Token).IsUnique();
            e.HasIndex(w => new { w.TenantId, w.Platform }).IsUnique();

            e.HasOne(w => w.Tenant)
             .WithMany()
             .HasForeignKey(w => w.TenantId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Subscription>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.AsaasSubscriptionId).HasMaxLength(100);
            e.Property(s => s.AsaasCustomerId).HasMaxLength(100);

            // Um tenant, uma assinatura.
            e.HasIndex(s => s.TenantId).IsUnique();
            e.HasIndex(s => s.AsaasSubscriptionId);

            e.HasOne(s => s.Tenant)
             .WithMany()
             .HasForeignKey(s => s.TenantId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        ApplyTenantFilters(modelBuilder);
    }

    /// <summary>
    /// Isolamento multi-tenant por padrão: toda entidade ITenantEntity só enxerga linhas
    /// do tenant da requisição. Vazar dado passa a exigir um IgnoreQueryFilters() explícito
    /// e visível no código, em vez de acontecer por esquecer um .Where().
    /// </summary>
    private void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>().HasQueryFilter(t => t.Id == _tenantContext.TenantId);
        modelBuilder.Entity<Customer>().HasQueryFilter(c => c.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<Instance>().HasQueryFilter(i => i.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<SystemUser>().HasQueryFilter(u => u.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<MessageTemplate>().HasQueryFilter(t => t.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<WhatsAppMessage>().HasQueryFilter(m => m.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<WebhookIntegration>().HasQueryFilter(w => w.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<Subscription>().HasQueryFilter(s => s.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<WebhookEvent>().HasQueryFilter(w => w.TenantId == _tenantContext.TenantId);
    }

    /// <summary>
    /// Carimba o TenantId em entidades novas que não o receberam, para que nenhuma linha
    /// nasça órfã e invisível ao próprio dono por causa do query filter.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_tenantContext.IsSet)
        {
            foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
            {
                if (entry.State == EntityState.Added && entry.Entity.TenantId == Guid.Empty)
                    entry.Property(nameof(ITenantEntity.TenantId)).CurrentValue = _tenantContext.TenantId;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
