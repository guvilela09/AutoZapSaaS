using AutoMapper;
using AutoZapSaaS.Application.Common.Interfaces;
using AutoZapSaaS.Application.DTOs;
using AutoZapSaaS.Application.Services.Interfaces;
using AutoZapSaaS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutoZapSaaS.Application.Services.Implementations;

public class TenantService : ITenantService
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<TenantService> _logger;

    public TenantService(IApplicationDbContext context, IMapper mapper, ILogger<TenantService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<TenantResponse> CreateAsync(CreateTenantRequest request)
    {
        // Checagem de duplicidade é global por natureza — precisa enxergar todos os tenants.
        var existing = await _context.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Email == request.Email || t.Document == request.Document);

        if (existing is not null)
            throw new InvalidOperationException("Já existe um tenant com este email ou documento.");

        var tenant = new Tenant(request.Name, request.Email, request.Document);
        _context.Tenants.Add(tenant);

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.AdminPassword);
        var user = new SystemUser(tenant.Id, request.AdminEmail, passwordHash, "Admin");
        _context.SystemUsers.Add(user);

        await _context.SaveChangesAsync(CancellationToken.None);

        _logger.LogInformation("Tenant criado: {TenantId} - {TenantName}", tenant.Id, tenant.Name);
        return _mapper.Map<TenantResponse>(tenant);
    }

    public async Task<TenantResponse?> GetByIdAsync(Guid id)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == id);
        return tenant is null ? null : _mapper.Map<TenantResponse>(tenant);
    }

    public async Task<TenantResponse?> UpdateAsync(Guid id, UpdateTenantRequest request)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == id);
        if (tenant is null) return null;

        typeof(Tenant).GetProperty(nameof(Tenant.Name))!.SetValue(tenant, request.Name);
        typeof(Tenant).GetProperty(nameof(Tenant.Email))!.SetValue(tenant, request.Email);
        typeof(Tenant).GetProperty(nameof(Tenant.Document))!.SetValue(tenant, request.Document);
        tenant.SetUpdatedAt();

        await _context.SaveChangesAsync(CancellationToken.None);
        return _mapper.Map<TenantResponse>(tenant);
    }

    public async Task<bool> DeactivateAsync(Guid id)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == id);
        if (tenant is null) return false;

        tenant.Deactivate();
        await _context.SaveChangesAsync(CancellationToken.None);
        return true;
    }

    public async Task<bool> ActivateAsync(Guid id)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == id);
        if (tenant is null) return false;

        typeof(Tenant).GetProperty(nameof(Tenant.IsActive))!.SetValue(tenant, true);
        tenant.SetUpdatedAt();
        await _context.SaveChangesAsync(CancellationToken.None);
        return true;
    }
}
