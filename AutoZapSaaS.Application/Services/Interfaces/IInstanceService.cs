using AutoZapSaaS.Application.DTOs;

namespace AutoZapSaaS.Application.Services.Interfaces;

public interface IInstanceService
{
    Task<InstanceResponse> CreateAsync(Guid tenantId, CreateInstanceRequest request);
    Task<InstanceResponse?> GetByIdAsync(Guid id);
    Task<List<InstanceResponse>> GetByTenantAsync(Guid tenantId);
    Task<InstanceResponse?> UpdateAsync(Guid id, UpdateInstanceRequest request);
    Task<bool> DeleteAsync(Guid id);
    Task<QrCodeResponse> GetQrCodeAsync(Guid id);
    Task<ConnectionStatusResponse> GetStatusAsync(Guid id);
    Task<bool> ConnectAsync(Guid id);
    Task<bool> DisconnectAsync(Guid id);
}
