namespace AutoZapSaaS.Application.DTOs;

public record LoginRequest(string Email, string Password);

public record LoginResponse(string Token, string Email, string Role, Guid TenantId);

public record RegisterRequest(string Name, string Email, string Document, string AdminEmail, string AdminPassword);
