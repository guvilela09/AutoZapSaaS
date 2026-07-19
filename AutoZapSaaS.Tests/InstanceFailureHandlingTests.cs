using AutoMapper;
using AutoZapSaaS.Application.Common;
using AutoZapSaaS.Application.Common.Interfaces;
using AutoZapSaaS.Application.DTOs;
using AutoZapSaaS.Application.Mappings;
using AutoZapSaaS.Application.Services.Implementations;
using AutoZapSaaS.Domain.Entities;
using AutoZapSaaS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoZapSaaS.Tests;

/// <summary>
/// Antes, uma falha da Evolution virava HTTP 200: a instância era gravada no banco,
/// o catch apenas logava um warning e o tenant só descobria o problema ao tentar usar.
/// </summary>
public class InstanceFailureHandlingTests
{
    private readonly DbContextOptions<ApplicationDbContext> _options;
    private readonly IMapper _mapper;
    private readonly Guid _tenantId = Guid.NewGuid();

    public InstanceFailureHandlingTests()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"autozap-falhas-{Guid.NewGuid()}")
            .Options;

        _mapper = new MapperConfiguration(cfg => cfg.AddProfile<AutoMapperProfile>()).CreateMapper();
    }

    private (InstanceService servico, ApplicationDbContext ctx) Montar(IEvolutionApiClient client)
    {
        var ctx = new ApplicationDbContext(_options, new FakeTenantContext(_tenantId));
        return (new InstanceService(ctx, client, _mapper, NullLogger<InstanceService>.Instance), ctx);
    }

    [Fact]
    public async Task Falha_ao_criar_na_Evolution_nao_deixa_instancia_fantasma_no_banco()
    {
        var (servico, ctx) = Montar(new EvolutionFalhando());

        await Assert.ThrowsAsync<EvolutionApiException>(() =>
            servico.CreateAsync(_tenantId, new CreateInstanceRequest("Zap", "sessao-x", "tok")));

        Assert.Empty(await ctx.Instances.ToListAsync());
    }

    [Fact]
    public async Task Criacao_bem_sucedida_persiste_a_instancia()
    {
        var (servico, ctx) = Montar(new EvolutionOk());

        var resultado = await servico.CreateAsync(_tenantId, new CreateInstanceRequest("Zap", "sessao-ok", "tok"));

        Assert.NotEqual(Guid.Empty, resultado.Id);
        Assert.Single(await ctx.Instances.ToListAsync());
    }

    [Fact]
    public async Task Exclusao_prossegue_mesmo_com_a_Evolution_fora_do_ar()
    {
        // Travar a exclusão deixaria o tenant preso com uma instância que não consegue remover.
        var (criar, _) = Montar(new EvolutionOk());
        var criada = await criar.CreateAsync(_tenantId, new CreateInstanceRequest("Zap", "sessao-del", "tok"));

        var (servico, ctx) = Montar(new EvolutionFalhando());

        Assert.True(await servico.DeleteAsync(criada.Id));
        Assert.Empty(await ctx.Instances.ToListAsync());
    }

    [Fact]
    public async Task Exclusao_remove_a_instancia_na_Evolution_e_nao_so_faz_logout()
    {
        var evolution = new EvolutionOk();
        var (criar, _) = Montar(evolution);
        var criada = await criar.CreateAsync(_tenantId, new CreateInstanceRequest("Zap", "sessao-rm", "tok"));

        var (servico, _) = Montar(evolution);
        await servico.DeleteAsync(criada.Id);

        // Antes chamava só logout, deixando a instância existindo na Evolution para sempre.
        Assert.Contains("sessao-rm", evolution.Removidas);
    }

    [Fact]
    public async Task Ler_QR_Code_marca_Connecting_e_nao_Connected()
    {
        // Exibir o QR Code não significa que alguém escaneou.
        var (criar, _) = Montar(new EvolutionOk());
        var criada = await criar.CreateAsync(_tenantId, new CreateInstanceRequest("Zap", "sessao-qr", "tok"));

        var (servico, ctx) = Montar(new EvolutionOk());
        await servico.GetQrCodeAsync(criada.Id);

        var instancia = await ctx.Instances.FirstAsync(i => i.Id == criada.Id);
        Assert.Equal(InstanceStatus.Connecting, instancia.Status);
    }

    [Fact]
    public async Task Status_sincroniza_com_a_Evolution_quando_ela_responde_open()
    {
        var (criar, _) = Montar(new EvolutionOk());
        var criada = await criar.CreateAsync(_tenantId, new CreateInstanceRequest("Zap", "sessao-st", "tok"));

        var (servico, ctx) = Montar(new EvolutionOk { Estado = "open" });
        var status = await servico.GetStatusAsync(criada.Id);

        Assert.Equal("open", status.Status);
        Assert.Equal(InstanceStatus.Connected, (await ctx.Instances.FirstAsync(i => i.Id == criada.Id)).Status);
    }

    [Fact]
    public async Task Status_degrada_para_o_ultimo_conhecido_se_a_Evolution_cair()
    {
        var (criar, _) = Montar(new EvolutionOk());
        var criada = await criar.CreateAsync(_tenantId, new CreateInstanceRequest("Zap", "sessao-fb", "tok"));

        var (servico, _) = Montar(new EvolutionFalhando());
        var status = await servico.GetStatusAsync(criada.Id);

        Assert.Equal(InstanceStatus.Disconnected.ToString(), status.Status);
    }

    private class EvolutionOk : IEvolutionApiClient
    {
        public string Estado { get; init; } = "connecting";
        public List<string> Removidas { get; } = new();
        public List<string> Deslogadas { get; } = new();

        public Task<string> CreateInstanceAsync(string i, string t) => Task.FromResult("{}");
        public Task<string> ConnectInstanceAsync(string i) => Task.FromResult("{}");
        public Task<byte[]> GetQrCodeAsync(string i) => Task.FromResult(new byte[] { 1, 2, 3 });
        public Task<bool> SendMessageAsync(string i, string p, string m) => Task.FromResult(true);
        public Task<string> GetConnectionStatusAsync(string i) => Task.FromResult(Estado);

        public Task<bool> DisconnectInstanceAsync(string i)
        {
            Deslogadas.Add(i);
            return Task.FromResult(true);
        }

        public Task<bool> DeleteInstanceAsync(string i)
        {
            Removidas.Add(i);
            return Task.FromResult(true);
        }
    }

    private class EvolutionFalhando : IEvolutionApiClient
    {
        private static EvolutionApiException Falha() =>
            new("Evolution fora do ar", System.Net.HttpStatusCode.ServiceUnavailable, "{}");

        public Task<string> CreateInstanceAsync(string i, string t) => throw Falha();
        public Task<string> ConnectInstanceAsync(string i) => throw Falha();
        public Task<byte[]> GetQrCodeAsync(string i) => throw Falha();
        public Task<bool> SendMessageAsync(string i, string p, string m) => throw Falha();
        public Task<string> GetConnectionStatusAsync(string i) => throw Falha();
        public Task<bool> DisconnectInstanceAsync(string i) => throw Falha();
        public Task<bool> DeleteInstanceAsync(string i) => throw Falha();
    }

    private sealed class FakeTenantContext : ITenantContext
    {
        public FakeTenantContext(Guid tenantId) => TenantId = tenantId;
        public Guid TenantId { get; private set; }
        public bool IsSet => TenantId != Guid.Empty;
        public void SetTenant(Guid tenantId) => TenantId = tenantId;
    }
}
