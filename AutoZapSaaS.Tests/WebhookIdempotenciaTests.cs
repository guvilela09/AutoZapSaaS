using System.Text.Json;
using AutoMapper;
using AutoZapSaaS.Application.Common.Interfaces;
using AutoZapSaaS.Application.Mappings;
using AutoZapSaaS.Application.Services.Implementations;
using AutoZapSaaS.Domain.Entities;
using AutoZapSaaS.Domain.Enums;
using AutoZapSaaS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoZapSaaS.Tests;

/// <summary>
/// Kiwify, Hotmart e Nuvemshop reenviam o mesmo webhook em timeout ou erro.
/// Sem deduplicacao o consumidor final recebe a mesma mensagem varias vezes
/// e a cota do plano e consumida a cada reentrega.
/// </summary>
public class WebhookIdempotenciaTests
{
    private readonly DbContextOptions<ApplicationDbContext> _options;
    private readonly IMapper _mapper;
    private readonly Guid _tenantId = Guid.NewGuid();

    private const string Payload =
        """{"customer_name":"Joana","customer_phone":"5511955554444","customer_email":"j@teste.com"}""";

    public WebhookIdempotenciaTests()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"autozap-idem-{Guid.NewGuid()}")
            .Options;

        _mapper = new MapperConfiguration(cfg => cfg.AddProfile<AutoMapperProfile>()).CreateMapper();

        using var seed = new ApplicationDbContext(_options, new FakeTenantContext(_tenantId));
        var tenant = new Tenant("Loja", "loja@teste.com", "123");
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(tenant, _tenantId);
        seed.Tenants.Add(tenant);
        seed.Subscriptions.Add(Subscription.Gratuita(_tenantId));

        // Instancia conectada: sem ela o servico nem chega a tentar enviar.
        var instancia = new Instance(_tenantId, "Zap", "sessao", "tok");
        instancia.SetConnected();
        seed.Instances.Add(instancia);
        seed.SaveChanges();
    }

    private (WebhookService servico, EvolutionSpy evolution, ApplicationDbContext ctx) Montar()
    {
        var ctx = new ApplicationDbContext(_options, new FakeTenantContext(_tenantId));
        var evolution = new EvolutionSpy();
        var limites = new PlanLimitService(ctx, NullLogger<PlanLimitService>.Instance);

        return (new WebhookService(ctx, _mapper, evolution, limites, NullLogger<WebhookService>.Instance),
                evolution, ctx);
    }

    private static JsonElement Json(string bruto) => JsonDocument.Parse(bruto).RootElement;

    [Fact]
    public async Task Reentrega_do_mesmo_webhook_nao_envia_a_mensagem_de_novo()
    {
        var (servico, evolution, _) = Montar();

        await servico.ProcessNuvemshopAsync(_tenantId, Json(Payload));
        await servico.ProcessNuvemshopAsync(_tenantId, Json(Payload));
        await servico.ProcessNuvemshopAsync(_tenantId, Json(Payload));

        Assert.Equal(1, evolution.Enviadas);
    }

    [Fact]
    public async Task Reentrega_nao_consome_cota_de_novo()
    {
        var (servico, _, ctx) = Montar();

        await servico.ProcessNuvemshopAsync(_tenantId, Json(Payload));
        await servico.ProcessNuvemshopAsync(_tenantId, Json(Payload));

        var assinatura = await ctx.Subscriptions.FirstAsync();
        Assert.Equal(1, assinatura.MensagensNoCiclo);
    }

    [Fact]
    public async Task Pedido_diferente_continua_sendo_processado()
    {
        // A deduplicacao nao pode engolir venda legitima.
        var (servico, evolution, _) = Montar();

        await servico.ProcessNuvemshopAsync(_tenantId, Json(Payload));
        await servico.ProcessNuvemshopAsync(_tenantId, Json(
            """{"customer_name":"Outro","customer_phone":"5511911112222","customer_email":"o@teste.com"}"""));

        Assert.Equal(2, evolution.Enviadas);
    }

    [Fact]
    public void Hash_do_payload_e_estavel_e_distingue_conteudo()
    {
        Assert.Equal(WebhookEvent.CalcularHash(Payload), WebhookEvent.CalcularHash(Payload));
        Assert.NotEqual(WebhookEvent.CalcularHash(Payload), WebhookEvent.CalcularHash(Payload + " "));
    }

    private class EvolutionSpy : IEvolutionApiClient
    {
        public int Enviadas { get; private set; }

        public Task<bool> SendMessageAsync(string i, string p, string m)
        {
            Enviadas++;
            return Task.FromResult(true);
        }

        public Task<string> CreateInstanceAsync(string i, string t) => Task.FromResult("{}");
        public Task<string> ConnectInstanceAsync(string i) => Task.FromResult("{}");
        public Task<byte[]> GetQrCodeAsync(string i) => Task.FromResult(new byte[] { 1 });
        public Task<string> GetConnectionStatusAsync(string i) => Task.FromResult("open");
        public Task<bool> DisconnectInstanceAsync(string i) => Task.FromResult(true);
        public Task<bool> DeleteInstanceAsync(string i) => Task.FromResult(true);
    }

    private sealed class FakeTenantContext : ITenantContext
    {
        public FakeTenantContext(Guid tenantId) => TenantId = tenantId;
        public Guid TenantId { get; private set; }
        public bool IsSet => TenantId != Guid.Empty;
        public void SetTenant(Guid tenantId) => TenantId = tenantId;
    }
}
