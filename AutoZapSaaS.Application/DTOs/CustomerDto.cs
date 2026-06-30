namespace AutoZapSaaS.Application.DTOs;

public record CreateCustomerRequest(string Name, string PhoneNumber, string Email, string Origin);

public record UpdateCustomerRequest(string Name, string PhoneNumber, string Email);

public record CustomerResponse(
    Guid Id,
    Guid TenantId,
    string Name,
    string PhoneNumber,
    string Email,
    string Origin,
    DateTime CreatedAt);
