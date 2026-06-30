namespace AutoZapSaaS.Application.Common.Interfaces;

public interface IEvolutionApiClient
{
    Task<string> CreateInstanceAsync(string instanceName, string token);
    Task<string> ConnectInstanceAsync(string instanceName);
    Task<byte[]> GetQrCodeAsync(string instanceName);
    Task<bool> SendMessageAsync(string instanceName, string phoneNumber, string message);
    Task<string> GetConnectionStatusAsync(string instanceName);
    Task<bool> DisconnectInstanceAsync(string instanceName);
}
