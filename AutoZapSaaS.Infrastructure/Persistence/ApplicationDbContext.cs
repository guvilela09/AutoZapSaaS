using AutoZapSaaS.Application.Common.Interfaces;
using AutoZapSaaS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoZapSaaS.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Instance> Instances => Set<Instance>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<SystemUser> SystemUsers => Set<SystemUser>();
    public DbSet<WebhookEvent> WebhookEvents => Set<WebhookEvent>();
    public DbSet<MessageTemplate> MessageTemplates => Set<MessageTemplate>();
    public DbSet<WhatsAppMessage> WhatsAppMessages => Set<WhatsAppMessage>();

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
            e.HasIndex(c => c.PhoneNumber);
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

            e.HasOne(m => m.Customer)
             .WithMany()
             .HasForeignKey(m => m.CustomerId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
