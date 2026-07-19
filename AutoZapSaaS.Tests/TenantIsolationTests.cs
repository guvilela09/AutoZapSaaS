using AutoZapSaaS.Application.Common.Interfaces;
using AutoZapSaaS.Domain.Entities;
using AutoZapSaaS.Domain.Enums;
using AutoZapSaaS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.InMemory.Internal;

namespace AutoZapSaaS.Tests;

/// <summary>
/// Reproduz o ataque cross-tenant que existia antes dos Global Query Filters:
/// um tenant autenticado usando o GUID de um recurso de outro tenant.
/// </summary>
public class TenantIsolationTests
{
    private readonly DbContextOptions<ApplicationDbContext> _options;

    private readonly Guid _tenantA = Guid.NewGuid();
    private readonly Guid _tenantB = Guid.NewGuid();

    private Guid _customerDoB;
    private Guid _instanceDeB;
    private Guid _templateDeB;

    public TenantIsolationTests()
    {
        // Banco isolado por instância de teste (o xUnit cria uma por [Fact]).
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"autozap-testes-{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        // Semeia dados dos dois tenants sem nenhum tenant ativo na "requisição".
        using var seed = new ApplicationDbContext(_options, new FakeTenantContext(Guid.Empty));

        seed.Tenants.Add(new Tenant("Empresa A", "a@teste.com", "111"));
        seed.Tenants.Add(new Tenant("Empresa B", "b@teste.com", "222"));

        var customerB = new Customer(_tenantB, "Cliente da B", "5511999999999", "cliente@b.com", "Nuvemshop");
        var instanceB = new Instance(_tenantB, "Zap da B", "sessao-b", "token-b");
        var templateB = new MessageTemplate(_tenantB, "Template da B", EventType.OrderCreated, "Olá {{nome}}");

        seed.Customers.Add(customerB);
        seed.Instances.Add(instanceB);
        seed.MessageTemplates.Add(templateB);
        seed.SaveChanges();

        _customerDoB = customerB.Id;
        _instanceDeB = instanceB.Id;
        _templateDeB = templateB.Id;
    }

    private ApplicationDbContext ContextoDe(Guid tenantId) =>
        new(_options, new FakeTenantContext(tenantId));

    [Fact]
    public async Task TenantA_nao_enxerga_cliente_do_TenantB_mesmo_sabendo_o_id()
    {
        using var ctx = ContextoDe(_tenantA);

        var achado = await ctx.Customers.FirstOrDefaultAsync(c => c.Id == _customerDoB);

        Assert.Null(achado);
    }

    [Fact]
    public async Task TenantA_nao_enxerga_instancia_do_TenantB_mesmo_sabendo_o_id()
    {
        // Era o pior caso: pegar a instância alheia dava acesso ao QR Code
        // e permitia conectar o WhatsApp de outra empresa.
        using var ctx = ContextoDe(_tenantA);

        var achado = await ctx.Instances.FirstOrDefaultAsync(i => i.Id == _instanceDeB);

        Assert.Null(achado);
    }

    [Fact]
    public async Task TenantA_nao_enxerga_template_do_TenantB_mesmo_sabendo_o_id()
    {
        using var ctx = ContextoDe(_tenantA);

        var achado = await ctx.MessageTemplates.FirstOrDefaultAsync(t => t.Id == _templateDeB);

        Assert.Null(achado);
    }

    [Fact]
    public async Task TenantA_nao_lista_nada_do_TenantB()
    {
        using var ctx = ContextoDe(_tenantA);

        Assert.Empty(await ctx.Customers.ToListAsync());
        Assert.Empty(await ctx.Instances.ToListAsync());
        Assert.Empty(await ctx.MessageTemplates.ToListAsync());
    }

    [Fact]
    public async Task TenantB_continua_enxergando_os_proprios_dados()
    {
        // O filtro não pode ser tão agressivo a ponto de quebrar o uso legítimo.
        using var ctx = ContextoDe(_tenantB);

        Assert.NotNull(await ctx.Customers.FirstOrDefaultAsync(c => c.Id == _customerDoB));
        Assert.NotNull(await ctx.Instances.FirstOrDefaultAsync(i => i.Id == _instanceDeB));
        Assert.NotNull(await ctx.MessageTemplates.FirstOrDefaultAsync(t => t.Id == _templateDeB));
    }

    [Fact]
    public async Task Requisicao_sem_tenant_nao_enxerga_nada()
    {
        // Token de webhook inválido / JWT ausente não pode virar acesso irrestrito.
        using var ctx = ContextoDe(Guid.Empty);

        Assert.Empty(await ctx.Customers.ToListAsync());
        Assert.Empty(await ctx.Instances.ToListAsync());
    }

    [Fact]
    public async Task Entidade_nova_recebe_o_tenant_da_requisicao()
    {
        using var ctx = ContextoDe(_tenantA);

        ctx.Customers.Add(new Customer { Name = "Novo", PhoneNumber = "5511888888888" });
        await ctx.SaveChangesAsync();

        var salvo = await ctx.Customers.FirstOrDefaultAsync(c => c.Name == "Novo");
        Assert.NotNull(salvo);
        Assert.Equal(_tenantA, salvo!.TenantId);
    }

    private sealed class FakeTenantContext : ITenantContext
    {
        public FakeTenantContext(Guid tenantId) => TenantId = tenantId;
        public Guid TenantId { get; private set; }
        public bool IsSet => TenantId != Guid.Empty;
        public void SetTenant(Guid tenantId) => TenantId = tenantId;
    }
}
