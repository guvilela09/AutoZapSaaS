using AutoZapSaaS.Application.DTOs;

namespace AutoZapSaaS.Application.Services.Interfaces;

public interface ICustomerService
{
    Task<CustomerResponse> CreateAsync(Guid tenantId, CreateCustomerRequest request);
    Task<CustomerResponse?> GetByIdAsync(Guid id);
    Task<List<CustomerResponse>> GetByTenantAsync(Guid tenantId);
    Task<CustomerResponse?> GetByPhoneAsync(Guid tenantId, string phoneNumber);
    Task<CustomerResponse?> UpdateAsync(Guid id, UpdateCustomerRequest request);
    Task<bool> DeleteAsync(Guid id);
}
