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
    private readonly IMapper _mapper;
    private readonly ILogger<InstanceService> _logger;

    public InstanceService(
        IApplicationDbContext context,
        IEvolutionApiClient evolutionClient,
        IMapper mapper,
        ILogger<InstanceService> logger)
    {
        _context = context;
        _evolutionClient = evolutionClient;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<InstanceResponse> CreateAsync(Guid tenantId, CreateInstanceRequest request)
    {
        var instance = new Instance(tenantId, request.Name, request.SessionName, request.Token);
        _context.Instances.Add(instance);
        await _context.SaveChangesAsync(CancellationToken.None);

        try
        {
            await _evolutionClient.CreateInstanceAsync(request.SessionName, request.Token);
            _logger.LogInformation("Instância Evolution criada: {SessionName}", request.SessionName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Instância DB criada, mas Evolution API falhou para {SessionName}", request.SessionName);
        }

        return _mapper.Map<InstanceResponse>(instance);
    }

    public async Task<InstanceResponse?> GetByIdAsync(Guid id)
    {
        var instance = await _context.Instances.FindAsync(id);
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
        var instance = await _context.Instances.FindAsync(id);
        if (instance is null) return null;

        typeof(Instance).GetProperty(nameof(Instance.Name))!.SetValue(instance, request.Name);
        typeof(Instance).GetProperty(nameof(Instance.Token))!.SetValue(instance, request.Token);
        instance.SetUpdatedAt();

        await _context.SaveChangesAsync(CancellationToken.None);
        return _mapper.Map<InstanceResponse>(instance);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var instance = await _context.Instances.FindAsync(id);
        if (instance is null) return false;

        try
        {
            await _evolutionClient.DisconnectInstanceAsync(instance.SessionName);
        }
        catch { }

        _context.Instances.Remove(instance);
        await _context.SaveChangesAsync(CancellationToken.None);
        return true;
    }

    public async Task<QrCodeResponse> GetQrCodeAsync(Guid id)
    {
        var instance = await _context.Instances.FindAsync(id)
            ?? throw new InvalidOperationException("Instância não encontrada.");

        await _evolutionClient.ConnectInstanceAsync(instance.SessionName);

        await Task.Delay(1500);

        var qrCodeBytes = await _evolutionClient.GetQrCodeAsync(instance.SessionName);
        instance.SetConnected();
        await _context.SaveChangesAsync(CancellationToken.None);

        return new QrCodeResponse(Convert.ToBase64String(qrCodeBytes));
    }

    public async Task<ConnectionStatusResponse> GetStatusAsync(Guid id)
    {
        var instance = await _context.Instances.FindAsync(id)
            ?? throw new InvalidOperationException("Instância não encontrada.");

        try
        {
            var status = await _evolutionClient.GetConnectionStatusAsync(instance.SessionName);
            return new ConnectionStatusResponse(status);
        }
        catch
        {
            return new ConnectionStatusResponse(instance.Status.ToString());
        }
    }

    public async Task<bool> ConnectAsync(Guid id)
    {
        var instance = await _context.Instances.FindAsync(id);
        if (instance is null) return false;

        await _evolutionClient.ConnectInstanceAsync(instance.SessionName);
        instance.SetConnected();
        await _context.SaveChangesAsync(CancellationToken.None);
        return true;
    }

    public async Task<bool> DisconnectAsync(Guid id)
    {
        var instance = await _context.Instances.FindAsync(id);
        if (instance is null) return false;

        await _evolutionClient.DisconnectInstanceAsync(instance.SessionName);
        instance.SetDisconnected();
        await _context.SaveChangesAsync(CancellationToken.None);
        return true;
    }
}
