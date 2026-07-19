using AutoMapper;
using AutoZapSaaS.Application.Common.Interfaces;
using AutoZapSaaS.Application.DTOs;
using AutoZapSaaS.Application.Services.Interfaces;
using AutoZapSaaS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutoZapSaaS.Application.Services.Implementations;

public class InstanceService : IInstanceService
{
    private readonly IApplicationDbContext _context;
    private readonly IEvolutionApiClient _evolutionClient;
    private readonly IPlanLimitService _planLimits;
    private readonly IMapper _mapper;
    private readonly ILogger<InstanceService> _logger;

    public InstanceService(
        IApplicationDbContext context,
        IEvolutionApiClient evolutionClient,
        IPlanLimitService planLimits,
        IMapper mapper,
        ILogger<InstanceService> logger)
    {
        _context = context;
        _evolutionClient = evolutionClient;
        _planLimits = planLimits;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<InstanceResponse> CreateAsync(Guid tenantId, CreateInstanceRequest request)
    {
        await _planLimits.GarantirPodeCriarInstanciaAsync(tenantId);

        var instance = new Instance(tenantId, request.Name, request.SessionName, request.Token);
        _context.Instances.Add(instance);
        await _context.SaveChangesAsync(CancellationToken.None);

        try
        {
            await _evolutionClient.CreateInstanceAsync(request.SessionName, request.Token);
            _logger.LogInformation("Instância Evolution criada: {SessionName}", request.SessionName);
        }
        catch (Exception)
        {
            // Uma instância que existe só no nosso banco é inútil: conectar, ler QR Code
            // e enviar mensagem falham todos depois. Antes isso virava um 200 e o tenant
            // só descobria o problema ao tentar usar. Desfaz e propaga.
            _context.Instances.Remove(instance);
            await _context.SaveChangesAsync(CancellationToken.None);
            throw;
        }

        return _mapper.Map<InstanceResponse>(instance);
    }

    public async Task<InstanceResponse?> GetByIdAsync(Guid id)
    {
        var instance = await _context.Instances.FirstOrDefaultAsync(i => i.Id == id);
        return instance is null ? null : _mapper.Map<InstanceResponse>(instance);
    }

    public async Task<List<InstanceResponse>> GetByTenantAsync(Guid tenantId)
    {
        var instances = await _context.Instances
            .Where(i => i.TenantId == tenantId)
            .ToListAsync();

        return _mapper.Map<List<InstanceResponse>>(instances);
    }

    public async Task<InstanceResponse?> UpdateAsync(Guid id, UpdateInstanceRequest request)
    {
        var instance = await _context.Instances.FirstOrDefaultAsync(i => i.Id == id);
        if (instance is null) return null;

        typeof(Instance).GetProperty(nameof(Instance.Name))!.SetValue(instance, request.Name);
        typeof(Instance).GetProperty(nameof(Instance.Token))!.SetValue(instance, request.Token);
        instance.SetUpdatedAt();

        await _context.SaveChangesAsync(CancellationToken.None);
        return _mapper.Map<InstanceResponse>(instance);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var instance = await _context.Instances.FirstOrDefaultAsync(i => i.Id == id);
        if (instance is null) return false;

        try
        {
            // Remove de verdade da Evolution. Antes chamava apenas logout, que desconecta
            // mas deixa a instância existindo lá para sempre — um vazamento por exclusão.
            await _evolutionClient.DeleteInstanceAsync(instance.SessionName);
        }
        catch (Exception ex)
        {
            // Não bloqueia a exclusão: se a Evolution está fora do ar, o tenant ficaria
            // preso com uma instância que não consegue remover. Mas registra como erro,
            // porque sobra uma sessão órfã lá que precisa de limpeza manual.
            _logger.LogError(ex,
                "Instância {SessionName} removida do banco, mas continua na Evolution. " +
                "Requer limpeza manual.", instance.SessionName);
        }

        _context.Instances.Remove(instance);
        await _context.SaveChangesAsync(CancellationToken.None);
        return true;
    }

    public async Task<QrCodeResponse> GetQrCodeAsync(Guid id)
    {
        var instance = await _context.Instances.FirstOrDefaultAsync(i => i.Id == id)
            ?? throw new InvalidOperationException("Instância não encontrada.");

        // GetQrCodeAsync já chama o connect por baixo — na v2 o QR vem nessa resposta.
        // O connect extra aqui, mais um Task.Delay(1500) fixo, era herança da v1.
        var qrCodeBytes = await _evolutionClient.GetQrCodeAsync(instance.SessionName);

        // Aguardando o scan — quem confirma a conexão de fato é o GetStatusAsync,
        // consultando a Evolution.
        instance.SetConnecting();
        await _context.SaveChangesAsync(CancellationToken.None);

        return new QrCodeResponse(Convert.ToBase64String(qrCodeBytes));
    }

    public async Task<ConnectionStatusResponse> GetStatusAsync(Guid id)
    {
        var instance = await _context.Instances.FirstOrDefaultAsync(i => i.Id == id)
            ?? throw new InvalidOperationException("Instância não encontrada.");

        try
        {
            var status = await _evolutionClient.GetConnectionStatusAsync(instance.SessionName);

            // A Evolution é a fonte da verdade: sincroniza o que guardamos.
            if (status.Equals("open", StringComparison.OrdinalIgnoreCase))
                instance.SetConnected();
            else if (status.Equals("close", StringComparison.OrdinalIgnoreCase))
                instance.SetDisconnected();

            await _context.SaveChangesAsync(CancellationToken.None);
            return new ConnectionStatusResponse(status);
        }
        catch (Exception ex)
        {
            // Degrada para o último estado conhecido em vez de derrubar a tela, mas
            // registra: sem isso, uma Evolution fora do ar parecia só "desconectado".
            _logger.LogWarning(ex,
                "Evolution API indisponível ao consultar status de {SessionName}. " +
                "Retornando último estado conhecido: {Status}", instance.SessionName, instance.Status);

            return new ConnectionStatusResponse(instance.Status.ToString());
        }
    }

    public async Task<bool> ConnectAsync(Guid id)
    {
        var instance = await _context.Instances.FirstOrDefaultAsync(i => i.Id == id);
        if (instance is null) return false;

        await _evolutionClient.ConnectInstanceAsync(instance.SessionName);

        // Só inicia o pareamento — a conexão real depende do scan do QR Code.
        instance.SetConnecting();
        await _context.SaveChangesAsync(CancellationToken.None);
        return true;
    }

    public async Task<bool> DisconnectAsync(Guid id)
    {
        var instance = await _context.Instances.FirstOrDefaultAsync(i => i.Id == id);
        if (instance is null) return false;

        await _evolutionClient.DisconnectInstanceAsync(instance.SessionName);
        instance.SetDisconnected();
        await _context.SaveChangesAsync(CancellationToken.None);
        return true;
    }
}
