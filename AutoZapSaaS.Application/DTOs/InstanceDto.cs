namespace AutoZapSaaS.Application.DTOs;

public record CreateInstanceRequest(string Name, string SessionName, string Token);

public record UpdateInstanceRequest(string Name, string Token);

public record InstanceResponse(
    Guid Id,
    Guid TenantId,
    string Name,
    string SessionName,
    string Token,
    string Status,
    DateTime CreatedAt);

public record QrCodeResponse(string QrCodeBase64);
public record ConnectionStatusResponse(string Status);
