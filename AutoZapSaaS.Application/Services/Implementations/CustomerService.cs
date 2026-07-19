using AutoMapper;
using AutoZapSaaS.Application.Common.Interfaces;
using AutoZapSaaS.Application.DTOs;
using AutoZapSaaS.Application.Services.Interfaces;
using AutoZapSaaS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutoZapSaaS.Application.Services.Implementations;

public class CustomerService : ICustomerService
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(IApplicationDbContext context, IMapper mapper, ILogger<CustomerService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<CustomerResponse> CreateAsync(Guid tenantId, CreateCustomerRequest request)
    {
        var customer = new Customer(tenantId, request.Name, request.PhoneNumber, request.Email, request.Origin);
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync(CancellationToken.None);

        _logger.LogInformation("Cliente criado: {CustomerId} - {Name}", customer.Id, customer.Name);
        return _mapper.Map<CustomerResponse>(customer);
    }

    public async Task<CustomerResponse?> GetByIdAsync(Guid id)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == id);
        return customer is null ? null : _mapper.Map<CustomerResponse>(customer);
    }

    public async Task<List<CustomerResponse>> GetByTenantAsync(Guid tenantId)
    {
        var customers = await _context.Customers
            .Where(c => c.TenantId == tenantId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return _mapper.Map<List<CustomerResponse>>(customers);
    }

    public async Task<CustomerResponse?> GetByPhoneAsync(Guid tenantId, string phoneNumber)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.PhoneNumber == phoneNumber);

        return customer is null ? null : _mapper.Map<CustomerResponse>(customer);
    }

    public async Task<CustomerResponse?> UpdateAsync(Guid id, UpdateCustomerRequest request)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == id);
        if (customer is null) return null;

        typeof(Customer).GetProperty(nameof(Customer.Name))!.SetValue(customer, request.Name);
        typeof(Customer).GetProperty(nameof(Customer.PhoneNumber))!.SetValue(customer, request.PhoneNumber);
        typeof(Customer).GetProperty(nameof(Customer.Email))!.SetValue(customer, request.Email);
        customer.SetUpdatedAt();

        await _context.SaveChangesAsync(CancellationToken.None);
        return _mapper.Map<CustomerResponse>(customer);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == id);
        if (customer is null) return false;

        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync(CancellationToken.None);
        return true;
    }
}
