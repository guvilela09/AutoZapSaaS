using AutoZapSaaS.Application.Common;
using AutoZapSaaS.Application.Common.Interfaces;
using AutoZapSaaS.Application.Services.Implementations;
using AutoZapSaaS.Domain.Entities;
using AutoZapSaaS.Domain.Enums;
using AutoZapSaaS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoZapSaaS.Tests;

/// <summary>
/// Limite de plano nao aplicado e receita vazando: o cliente do Free usaria
/// o produto inteiro sem pagar.
/// </summary>
public class PlanLimitTests
{
    private readonly DbContextOptions<ApplicationDbContext> _options;
    private readonly Guid _tenantId = Guid.NewGuid();

    public PlanLimitTests()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"autozap-planos-{Guid.NewGuid()}")
            .Options;
    }

    private ApplicationDbContext Ctx() => new(_options, new FakeTenantContext(_tenantId));

    private PlanLimitService Servico(ApplicationDbContext ctx) =>
        new(ctx, NullLogger<PlanLimitService>.Instance);

    private async Task<Subscription> AssinaturaAsync(ApplicationDbContext ctx, PlanTier tier)
    {
        var assinatura = Subscription.Gratuita(_tenantId);
        if (tier != PlanTier.Free)
            assinatura.IniciarCobranca(tier, "cus_1", "sub_1");

        ctx.Subscriptions.Add(assinatura);
        await ctx.SaveChangesAsync();
        return assinatura;
    }

    [Fact]
    public async Task Tenant_sem_assinatura_cai_no_Free_em_vez_de_ficar_sem_limite()
    {
        using var ctx = Ctx();

        var assinatura = await Servico(ctx).ObterOuCriarAsync(_tenantId);

        Assert.Equal(PlanTier.Free, assinatura.Tier);
        Assert.Equal(PlanCatalog.Free.MaxInstancias, assinatura.Plano.MaxInstancias);
    }

    [Fact]
    public async Task Free_bloqueia_a_segunda_instancia()
    {
        using var ctx = Ctx();
        await AssinaturaAsync(ctx, PlanTier.Free);

        ctx.Instances.Add(new Instance(_tenantId, "Zap 1", "s1", "t1"));
        await ctx.SaveChangesAsync();

        var erro = await Assert.ThrowsAsync<PlanLimitException>(() =>
            Servico(ctx).GarantirPodeCriarInstanciaAsync(_tenantId));

        Assert.Equal("Free", erro.PlanoAtual);
        Assert.Equal("Pro", erro.PlanoSugerido);
    }

    [Fact]
    public async Task Pro_permite_mais_instancias_que_o_Free()
    {
        using var ctx = Ctx();
        await AssinaturaAsync(ctx, PlanTier.Pro);

        ctx.Instances.Add(new Instance(_tenantId, "Zap 1", "s1", "t1"));
        await ctx.SaveChangesAsync();

        // No Free isso ja teria estourado.
        await Servico(ctx).GarantirPodeCriarInstanciaAsync(_tenantId);
    }

    [Fact]
    public async Task Cota_de_mensagens_do_Free_se_esgota_e_bloqueia()
    {
        using var ctx = Ctx();
        await AssinaturaAsync(ctx, PlanTier.Free);
        var servico = Servico(ctx);

        for (var i = 0; i < PlanCatalog.Free.MaxMensagensPorMes; i++)
            await servico.ConsumirMensagemAsync(_tenantId);

        await Assert.ThrowsAsync<PlanLimitException>(() => servico.ConsumirMensagemAsync(_tenantId));
    }

    [Fact]
    public void Ciclo_de_mensagens_vira_depois_de_um_mes()
    {
        var assinatura = Subscription.Gratuita(_tenantId);

        for (var i = 0; i < PlanCatalog.Free.MaxMensagensPorMes; i++)
            Assert.True(assinatura.TentarConsumirMensagem());

        Assert.False(assinatura.TentarConsumirMensagem());

        // Empurra o inicio do ciclo para 31 dias atras.
        typeof(Subscription).GetProperty(nameof(Subscription.CicloIniciadoEm))!
            .SetValue(assinatura, DateTime.UtcNow.AddDays(-31));

        Assert.True(assinatura.TentarConsumirMensagem());
        Assert.Equal(PlanCatalog.Free.MaxMensagensPorMes - 1, assinatura.MensagensRestantes());
    }

    [Fact]
    public void Assinatura_atrasada_mantem_acesso_e_suspensa_perde()
    {
        var assinatura = Subscription.Gratuita(_tenantId);
        assinatura.IniciarCobranca(PlanTier.Pro, "cus_1", "sub_1");
        assinatura.ConfirmarPagamento(DateTime.UtcNow.AddMonths(1));
        Assert.True(assinatura.EstaAtiva);

        // Atraso de boleto nao pode cortar o WhatsApp do lojista na hora.
        assinatura.MarcarAtrasada();
        Assert.True(assinatura.EstaAtiva);
        Assert.Equal(PlanTier.Pro, assinatura.Tier);

        assinatura.Suspender();
        Assert.False(assinatura.EstaAtiva);
        Assert.Equal(PlanTier.Free, assinatura.Tier);
    }

    [Fact]
    public void Cancelamento_rebaixa_para_Free_e_limpa_a_assinatura_no_Asaas()
    {
        var assinatura = Subscription.Gratuita(_tenantId);
        assinatura.IniciarCobranca(PlanTier.Business, "cus_1", "sub_1");
        assinatura.ConfirmarPagamento(DateTime.UtcNow.AddMonths(1));

        assinatura.Cancelar();

        Assert.Equal(PlanTier.Free, assinatura.Tier);
        Assert.Null(assinatura.AsaasSubscriptionId);
        Assert.Null(assinatura.PeriodoFimEm);
    }

    private sealed class FakeTenantContext : ITenantContext
    {
        public FakeTenantContext(Guid tenantId) => TenantId = tenantId;
        public Guid TenantId { get; private set; }
        public bool IsSet => TenantId != Guid.Empty;
        public void SetTenant(Guid tenantId) => TenantId = tenantId;
    }
}
