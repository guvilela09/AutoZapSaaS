using AutoZapSaaS.Application.DTOs;

namespace AutoZapSaaS.Application.Services.Interfaces;

public interface ITenantService
{
    Task<TenantResponse> CreateAsync(CreateTenantRequest request);
    Task<TenantResponse?> GetByIdAsync(Guid id);
    Task<TenantResponse?> UpdateAsync(Guid id, UpdateTenantRequest request);
    Task<bool> DeactivateAsync(Guid id);
    Task<bool> ActivateAsync(Guid id);
}
