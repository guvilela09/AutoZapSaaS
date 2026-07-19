using AutoZapSaaS.Application.Common.Interfaces;
using AutoZapSaaS.Domain.Entities;
using AutoZapSaaS.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AutoZapSaaS.Tests;

/// <summary>
/// Usa SQLite (e não o provider InMemory) de propósito: o bug corrigido aqui era uma
/// violação de foreign key, que só um banco relacional de verdade consegue reproduzir.
/// Antes, toda mensagem nascia com CustomerId = Guid.Empty e o INSERT estourava.
/// </summary>
public class WhatsAppMessageCustomerLinkTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ApplicationDbContext> _options;
    private readonly Guid _tenantId = Guid.NewGuid();

    private Guid _instanceId;
    private Guid _customerId;

    public WhatsAppMessageCustomerLinkTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var seed = new SqliteApplicationDbContext(_options, new FakeTenantContext(_tenantId));
        seed.Database.EnsureCreated();

        var tenant = new Tenant("Empresa", "empresa@teste.com", "123");
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(tenant, _tenantId);
        seed.Tenants.Add(tenant);

        var instance = new Instance(_tenantId, "Zap", "sessao", "token");
        var customer = new Customer(_tenantId, "Fulano", "5511999999999", "f@teste.com", "Nuvemshop");
        seed.Instances.Add(instance);
        seed.Customers.Add(customer);
        seed.SaveChanges();

        _instanceId = instance.Id;
        _customerId = customer.Id;
    }

    private SqliteApplicationDbContext Contexto() =>
        new(_options, new FakeTenantContext(_tenantId));

    [Fact]
    public async Task Mensagem_sem_cliente_vinculado_e_persistida()
    {
        // Envio avulso para um telefone que não é cliente cadastrado.
        using var ctx = Contexto();

        ctx.WhatsAppMessages.Add(
            new WhatsAppMessage(_tenantId, _instanceId, null, "5511888888888", "Olá"));

        await ctx.SaveChangesAsync();

        var salva = await ctx.WhatsAppMessages.SingleAsync();
        Assert.Null(salva.CustomerId);
    }

    [Fact]
    public async Task Mensagem_do_webhook_fica_vinculada_ao_cliente()
    {
        using var ctx = Contexto();

        ctx.WhatsAppMessages.Add(
            new WhatsAppMessage(_tenantId, _instanceId, _customerId, "5511999999999", "Pedido confirmado"));

        await ctx.SaveChangesAsync();

        var salva = await ctx.WhatsAppMessages.SingleAsync();
        Assert.Equal(_customerId, salva.CustomerId);
    }

    [Fact]
    public async Task CustomerId_inexistente_e_rejeitado_pelo_banco()
    {
        // Guarda-costas do bug antigo: Guid.Empty era exatamente este caso.
        using var ctx = Contexto();

        ctx.WhatsAppMessages.Add(
            new WhatsAppMessage(_tenantId, _instanceId, Guid.NewGuid(), "5511777777777", "Olá"));

        await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
    }

    [Fact]
    public async Task Apagar_cliente_preserva_o_historico_de_mensagens()
    {
        using (var ctx = Contexto())
        {
            ctx.WhatsAppMessages.Add(
                new WhatsAppMessage(_tenantId, _instanceId, _customerId, "5511999999999", "Histórico"));
            await ctx.SaveChangesAsync();
        }

        using (var ctx = Contexto())
        {
            ctx.Customers.Remove(await ctx.Customers.SingleAsync(c => c.Id == _customerId));
            await ctx.SaveChangesAsync();
        }

        using (var ctx = Contexto())
        {
            var mensagem = await ctx.WhatsAppMessages.SingleAsync();
            Assert.Equal("Histórico", mensagem.Body);
            Assert.Null(mensagem.CustomerId);
        }
    }

    public void Dispose() => _connection.Dispose();

    /// <summary>
    /// O modelo declara nvarchar(max), que é sintaxe de SQL Server. Traduz para TEXT
    /// apenas no teste — o mapeamento de produção fica intacto.
    /// </summary>
    private sealed class SqliteApplicationDbContext : ApplicationDbContext
    {
        public SqliteApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options, ITenantContext tenantContext)
            : base(options, tenantContext) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            foreach (var property in modelBuilder.Model.GetEntityTypes()
                         .SelectMany(t => t.GetProperties())
                         .Where(p => p.GetColumnType() == "nvarchar(max)"))
            {
                property.SetColumnType("TEXT");
            }
        }
    }

    private sealed class FakeTenantContext : ITenantContext
    {
        public FakeTenantContext(Guid tenantId) => TenantId = tenantId;
        public Guid TenantId { get; private set; }
        public bool IsSet => TenantId != Guid.Empty;
        public void SetTenant(Guid tenantId) => TenantId = tenantId;
    }
}
