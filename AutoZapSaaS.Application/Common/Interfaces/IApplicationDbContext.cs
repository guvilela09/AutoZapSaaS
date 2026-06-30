using AutoZapSaaS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoZapSaaS.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<Instance> Instances { get; }
    DbSet<Customer> Customers { get; }
    DbSet<SystemUser> SystemUsers { get; }
    DbSet<WebhookEvent> WebhookEvents { get; }
    DbSet<MessageTemplate> MessageTemplates { get; }
    DbSet<WhatsAppMessage> WhatsAppMessages { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
