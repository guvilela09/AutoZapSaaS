using AutoZapSaaS.Application.Common.Interfaces;
using AutoZapSaaS.Application.DTOs;
using AutoZapSaaS.Application.Services.Implementations;
using AutoZapSaaS.Domain.Entities;
using AutoZapSaaS.Domain.Enums;
using AutoZapSaaS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoZapSaaS.Tests;

public class SubscriptionTests
{
    private readonly DbContextOptions<ApplicationDbContext> _options;
    private readonly Guid _tenantId = Guid.NewGuid();

    public SubscriptionTests()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"autozap-assinaturas-{Guid.NewGuid()}")
            .Options;

        using var seed = new ApplicationDbContext(_options, new FakeTenantContext(Guid.Empty));
        var tenant = new Tenant("Loja", "loja@teste.com", "12345678901");
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(tenant, _tenantId);
        seed.Tenants.Add(tenant);
        seed.SaveChanges();
    }

    private (SubscriptionService servico, ApplicationDbContext ctx, AsaasFake asaas) Montar()
    {
        var ctx = new ApplicationDbContext(_options, new FakeTenantContext(_tenantId));
        var asaas = new AsaasFake();
        var limites = new PlanLimitService(ctx, NullLogger<PlanLimitService>.Instance);

        return (new SubscriptionService(ctx, asaas, limites, NullLogger<SubscriptionService>.Instance), ctx, asaas);
    }

    [Fact]
    public async Task Tenant_novo_comeca_no_Free()
    {
        var (servico, _, _) = Montar();

        var atual = await servico.ObterAtualAsync(_tenantId);

        Assert.Equal("Free", atual.Tier);
        Assert.Equal(PlanCatalog.Free.MaxMensagensPorMes, atual.MensagensRestantes);
    }

    [Fact]
    public async Task Assinar_Pro_cria_cliente_e_assinatura_no_Asaas()
    {
        var (servico, _, asaas) = Montar();

        var resultado = await servico.AssinarAsync(_tenantId, new AssinarRequest("Pro", "Pix"));

        Assert.Equal("Pro", resultado.Tier);
        Assert.Equal(SubscriptionStatus.Pending.ToString(), resultado.Status);
        Assert.Single(asaas.ClientesCriados);
        Assert.Single(asaas.AssinaturasCriadas);
    }

    [Fact]
    public async Task Plano_so_libera_limites_apos_a_confirmacao_de_pagamento()
    {
        var (servico, ctx, _) = Montar();
        await servico.AssinarAsync(_tenantId, new AssinarRequest("Pro", "Pix"));

        var assinatura = await ctx.Subscriptions.FirstAsync();
        Assert.Equal(SubscriptionStatus.Pending, assinatura.Status);
        Assert.False(assinatura.EstaAtiva);

        await servico.AplicarEventoDeCobrancaAsync(
            "PAYMENT_CONFIRMED", assinatura.AsaasSubscriptionId!, DateTime.UtcNow.AddMonths(1));

        var depois = await ctx.Subscriptions.FirstAsync();
        Assert.Equal(SubscriptionStatus.Active, depois.Status);
        Assert.True(depois.EstaAtiva);
    }

    [Fact]
    public async Task Atraso_mantem_o_plano_e_cancelamento_rebaixa()
    {
        var (servico, ctx, _) = Montar();
        await servico.AssinarAsync(_tenantId, new AssinarRequest("Business", "Boleto"));
        var asaasId = (await ctx.Subscriptions.FirstAsync()).AsaasSubscriptionId!;

        await servico.AplicarEventoDeCobrancaAsync("PAYMENT_CONFIRMED", asaasId, DateTime.UtcNow.AddMonths(1));
        await servico.AplicarEventoDeCobrancaAsync("PAYMENT_OVERDUE", asaasId, null);

        var atrasada = await ctx.Subscriptions.FirstAsync();
        Assert.Equal(PlanTier.Business, atrasada.Tier);
        Assert.True(atrasada.EstaAtiva);

        await servico.AplicarEventoDeCobrancaAsync("SUBSCRIPTION_DELETED", asaasId, null);

        var cancelada = await ctx.Subscriptions.FirstAsync();
        Assert.Equal(PlanTier.Free, cancelada.Tier);
    }

    [Fact]
    public async Task Evento_de_assinatura_desconhecida_nao_altera_ninguem()
    {
        // Impede que um evento forjado libere plano pago para outro tenant.
        var (servico, ctx, _) = Montar();
        await servico.AssinarAsync(_tenantId, new AssinarRequest("Pro", "Pix"));

        await servico.AplicarEventoDeCobrancaAsync("PAYMENT_CONFIRMED", "sub_inexistente", null);

        var assinatura = await ctx.Subscriptions.FirstAsync();
        Assert.Equal(SubscriptionStatus.Pending, assinatura.Status);
    }

    [Fact]
    public async Task Trocar_de_plano_cancela_a_cobranca_anterior()
    {
        // Sem isso o lojista ficaria com duas assinaturas ativas no Asaas.
        var (servico, _, asaas) = Montar();

        await servico.AssinarAsync(_tenantId, new AssinarRequest("Pro", "Pix"));
        var primeira = asaas.AssinaturasCriadas[0];

        await servico.AssinarAsync(_tenantId, new AssinarRequest("Business", "Pix"));

        Assert.Contains(primeira, asaas.AssinaturasCanceladas);
        Assert.Equal(2, asaas.AssinaturasCriadas.Count);
        // Cliente e reaproveitado entre trocas.
        Assert.Single(asaas.ClientesCriados);
    }

    [Fact]
    public async Task Nao_deixa_assinar_o_Free()
    {
        var (servico, _, _) = Montar();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            servico.AssinarAsync(_tenantId, new AssinarRequest("Free", "Pix")));
    }

    private class AsaasFake : IAsaasClient
    {
        public List<string> ClientesCriados { get; } = new();
        public List<string> AssinaturasCriadas { get; } = new();
        public List<string> AssinaturasCanceladas { get; } = new();

        public Task<AsaasCliente> CriarOuAtualizarClienteAsync(
            string nome, string email, string cpfCnpj, CancellationToken ct = default)
        {
            var id = $"cus_{ClientesCriados.Count + 1}";
            ClientesCriados.Add(id);
            return Task.FromResult(new AsaasCliente(id));
        }

        public Task<AsaasAssinatura> CriarAssinaturaAsync(
            string customerId, decimal valor, string descricao,
            AsaasFormaPagamento forma, CancellationToken ct = default)
        {
            var id = $"sub_{AssinaturasCriadas.Count + 1}";
            AssinaturasCriadas.Add(id);
            return Task.FromResult(new AsaasAssinatura(id, DateTime.UtcNow.AddMonths(1)));
        }

        public Task CancelarAssinaturaAsync(string subscriptionId, CancellationToken ct = default)
        {
            AssinaturasCanceladas.Add(subscriptionId);
            return Task.CompletedTask;
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
