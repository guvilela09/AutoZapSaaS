using AutoMapper;
using AutoZapSaaS.Application.Common.Interfaces;
using AutoZapSaaS.Application.DTOs;
using AutoZapSaaS.Application.Services.Interfaces;
using AutoZapSaaS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoZapSaaS.Application.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IMapper _mapper;

    public AuthService(IApplicationDbContext context, IJwtService jwtService, IMapper mapper)
    {
        _context = context;
        _jwtService = jwtService;
        _mapper = mapper;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        // Login acontece antes de existir tenant na requisição: precisa varrer todos os
        // usuários para descobrir a qual tenant o e-mail pertence.
        var user = await _context.SystemUsers
            .IgnoreQueryFilters()
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Email ou senha inválidos.");

        if (!user.Tenant.IsActive)
            throw new UnauthorizedAccessException("Conta desativada.");

        var token = _jwtService.GenerateToken(user.Id, user.TenantId, user.Email, user.Role);

        return new LoginResponse(token, user.Email, user.Role, user.TenantId);
    }

    public async Task<LoginResponse> RegisterAsync(RegisterRequest request)
    {
        // Checagem de duplicidade é global por natureza — precisa enxergar todos os tenants.
        var existingTenant = await _context.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Email == request.Email || t.Document == request.Document);

        if (existingTenant is not null)
            throw new InvalidOperationException("Já existe uma conta com este email ou documento.");

        var existingUser = await _context.SystemUsers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == request.AdminEmail);

        if (existingUser is not null)
            throw new InvalidOperationException("Email de administrador já está em uso.");

        var tenant = new Tenant(request.Name, request.Email, request.Document);
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync(CancellationToken.None);

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.AdminPassword);
        var user = new SystemUser(tenant.Id, request.AdminEmail, passwordHash, "Admin");
        _context.SystemUsers.Add(user);

        // Todo tenant nasce no Free: sem assinatura, os limites ficariam indefinidos.
        _context.Subscriptions.Add(Subscription.Gratuita(tenant.Id));

        await _context.SaveChangesAsync(CancellationToken.None);

        var token = _jwtService.GenerateToken(user.Id, tenant.Id, user.Email, user.Role);
        return new LoginResponse(token, user.Email, user.Role, tenant.Id);
    }
}
