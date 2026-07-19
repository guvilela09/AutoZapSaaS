using AutoZapSaaS.Application.Common;
using AutoZapSaaS.Application.Common.Interfaces;
using AutoZapSaaS.Application.DTOs;
using AutoZapSaaS.Application.Services.Implementations;
using AutoZapSaaS.Domain.Entities;
using AutoZapSaaS.Domain.Enums;
using AutoZapSaaS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AutoZapSaaS.Tests;

/// <summary>
/// O webhook é público: a única coisa que amarra a requisição a um tenant é o token
/// da URL. Estes testes cobrem essa resolução.
/// </summary>
public class WebhookIntegrationResolutionTests
{
    private readonly DbContextOptions<ApplicationDbContext> _options;
    private readonly Guid _tenantA = Guid.NewGuid();
    private readonly Guid _tenantB = Guid.NewGuid();
    private readonly string _tokenDeB;

    public WebhookIntegrationResolutionTests()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"autozap-webhooks-{Guid.NewGuid()}")
            .Options;

        using var seed = new ApplicationDbContext(_options, new FakeTenantContext(Guid.Empty));

        var integracaoB = new WebhookIntegration(_tenantB, WebhookPlatform.Nuvemshop, "secret-da-b");
        seed.WebhookIntegrations.Add(integracaoB);
        seed.SaveChanges();

        _tokenDeB = integracaoB.Token;
    }

    private WebhookIntegrationService ServicoSemTenant()
    {
        var ctx = new ApplicationDbContext(_options, new FakeTenantContext(Guid.Empty));
        var settings = Options.Create(new AppSettings { PublicBaseUrl = "https://api.teste.com" });

        return new WebhookIntegrationService(ctx, settings, NullLogger<WebhookIntegrationService>.Instance);
    }

    [Fact]
    public async Task Token_valido_resolve_o_tenant_dono()
    {
        var resultado = await ServicoSemTenant().ResolveByTokenAsync(_tokenDeB);

        Assert.NotNull(resultado);
        Assert.Equal(_tenantB, resultado!.TenantId);
        Assert.Equal(WebhookPlatform.Nuvemshop, resultado.Platform);
    }

    [Fact]
    public async Task Token_desconhecido_nao_resolve_nenhum_tenant()
    {
        // Antes, sem token, o sistema caía num Tenants.FirstOrDefault() e disparava
        // mensagem pela instância de um tenant qualquer.
        var resultado = await ServicoSemTenant().ResolveByTokenAsync("token-inventado");

        Assert.Null(resultado);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Token_vazio_nao_resolve_nenhum_tenant(string token)
    {
        Assert.Null(await ServicoSemTenant().ResolveByTokenAsync(token));
    }

    [Fact]
    public async Task Tenant_so_lista_as_proprias_integracoes()
    {
        var ctx = new ApplicationDbContext(_options, new FakeTenantContext(_tenantA));
        var settings = Options.Create(new AppSettings { PublicBaseUrl = "https://api.teste.com" });
        var servico = new WebhookIntegrationService(ctx, settings, NullLogger<WebhookIntegrationService>.Instance);

        Assert.Empty(await servico.GetByTenantAsync(_tenantA));
    }

    [Fact]
    public async Task Tenant_nao_consegue_rotacionar_token_de_outro()
    {
        var ctx = new ApplicationDbContext(_options, new FakeTenantContext(_tenantA));
        var settings = Options.Create(new AppSettings { PublicBaseUrl = "https://api.teste.com" });
        var servico = new WebhookIntegrationService(ctx, settings, NullLogger<WebhookIntegrationService>.Instance);

        var integracaoDeB = await new ApplicationDbContext(_options, new FakeTenantContext(_tenantB))
            .WebhookIntegrations.FirstAsync();

        Assert.Null(await servico.RotateTokenAsync(integracaoDeB.Id));
        Assert.False(await servico.DeleteAsync(integracaoDeB.Id));
    }

    [Fact]
    public void Tokens_gerados_nao_se_repetem()
    {
        var tokens = Enumerable.Range(0, 200)
            .Select(_ => new WebhookIntegration(_tenantA, WebhookPlatform.Kiwify, "s").Token)
            .ToList();

        Assert.Equal(tokens.Count, tokens.Distinct().Count());
        Assert.All(tokens, t => Assert.True(t.Length >= 32));
    }

    private sealed class FakeTenantContext : ITenantContext
    {
        public FakeTenantContext(Guid tenantId) => TenantId = tenantId;
        public Guid TenantId { get; private set; }
        public bool IsSet => TenantId != Guid.Empty;
        public void SetTenant(Guid tenantId) => TenantId = tenantId;
    }
}
