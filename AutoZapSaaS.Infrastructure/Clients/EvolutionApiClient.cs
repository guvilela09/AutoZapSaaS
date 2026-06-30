using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AutoZapSaaS.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoZapSaaS.Infrastructure.Clients;

public class EvolutionApiClient : IEvolutionApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EvolutionApiClient> _logger;
    private readonly EvolutionApiSettings _settings;

    public EvolutionApiClient(HttpClient httpClient, IOptions<EvolutionApiSettings> settings, ILogger<EvolutionApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task<string> CreateInstanceAsync(string instanceName, string token)
    {
        var payload = new
        {
            instanceName,
            token,
            qrcode = true,
            integration = new { type = "WHATSAPP-BAILEYS" }
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"{_settings.BaseUrl}/instance/create", payload);

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("Instância criada na Evolution: {Instance}", instanceName);
        return content;
    }

    public async Task<string> ConnectInstanceAsync(string instanceName)
    {
        SetHeaders(instanceName);
        var response = await _httpClient.GetAsync(
            $"{_settings.BaseUrl}/instance/connect/{instanceName}");

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("Conexão iniciada para: {Instance}", instanceName);
        return content;
    }

    public async Task<byte[]> GetQrCodeAsync(string instanceName)
    {
        SetHeaders(instanceName);
        var response = await _httpClient.GetAsync(
            $"{_settings.BaseUrl}/instance/qrcode-base64/{instanceName}");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var base64 = body.GetProperty("qrcode").GetString()
            ?? throw new InvalidOperationException("QR Code não encontrado na resposta.");

        return Convert.FromBase64String(base64);
    }

    public async Task<bool> SendMessageAsync(string instanceName, string phoneNumber, string message)
    {
        try
        {
            SetHeaders(instanceName);

            var payload = new { number = CleanPhoneNumber(phoneNumber), text = message };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_settings.BaseUrl}/message/sendText/{instanceName}", payload);

            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar mensagem para {Phone} via instância {Instance}", phoneNumber, instanceName);
            return false;
        }
    }

    public async Task<string> GetConnectionStatusAsync(string instanceName)
    {
        SetHeaders(instanceName);
        var response = await _httpClient.GetAsync(
            $"{_settings.BaseUrl}/instance/connectionState/{instanceName}");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        if (body.TryGetProperty("instance", out var instance) &&
            instance.TryGetProperty("state", out var state))
        {
            return state.GetString() ?? "disconnected";
        }

        return "disconnected";
    }

    public async Task<bool> DisconnectInstanceAsync(string instanceName)
    {
        try
        {
            SetHeaders(instanceName);
            var response = await _httpClient.DeleteAsync(
                $"{_settings.BaseUrl}/instance/logout/{instanceName}");

            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao desconectar instância: {Instance}", instanceName);
            return false;
        }
    }

    private void SetHeaders(string instanceName)
    {
        _httpClient.DefaultRequestHeaders.Remove("apikey");
        _httpClient.DefaultRequestHeaders.Add("apikey", _settings.ApiKey);
    }

    private static string CleanPhoneNumber(string phone)
    {
        return new string(phone.Where(char.IsDigit).ToArray());
    }
}

public class EvolutionApiSettings
{
    public string BaseUrl { get; set; } = "http://localhost:8080";
    public string ApiKey { get; set; } = string.Empty;
}
