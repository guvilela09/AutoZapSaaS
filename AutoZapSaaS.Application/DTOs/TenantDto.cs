namespace AutoZapSaaS.Application.DTOs;

public record CreateTenantRequest(string Name, string Email, string Document, string AdminEmail, string AdminPassword);

public record UpdateTenantRequest(string Name, string Email, string Document);

public record TenantResponse(Guid Id, string Name, string Email, string Document, bool IsActive, DateTime CreatedAt);

public record LoginRequest(string Email, string Password);

public record LoginResponse(string Token, string Email, string Role, Guid TenantId);

public record RegisterRequest(string Name, string Email, string Document, string AdminEmail, string AdminPassword);
