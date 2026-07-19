using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AutoZapSaaS.Application.Common;
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
            // Na Evolution v2 este campo e string. Como objeto, responde
            // 400 "Invalid integration".
            integration = "WHATSAPP-BAILEYS"
        };

        var response = await SendAsync(
            () => _httpClient.PostAsJsonAsync($"{_settings.BaseUrl}/instance/create", payload),
            "criar instância", instanceName);

        var content = await EnsureSuccessAsync(response, "criar instância", instanceName);
        _logger.LogInformation("Instância criada na Evolution: {Instance}", instanceName);
        return content;
    }

    public async Task<string> ConnectInstanceAsync(string instanceName)
    {
        var response = await SendAsync(
            () => _httpClient.GetAsync($"{_settings.BaseUrl}/instance/connect/{instanceName}"),
            "conectar instância", instanceName);

        var content = await EnsureSuccessAsync(response, "conectar instância", instanceName);
        _logger.LogInformation("Conexão iniciada para: {Instance}", instanceName);
        return content;
    }

    public async Task<byte[]> GetQrCodeAsync(string instanceName)
    {
        // Na v2 o QR Code vem na resposta do connect, campo "base64".
        // O endpoint /instance/qrcode-base64 da v1 não existe mais (404).
        var response = await SendAsync(
            () => _httpClient.GetAsync($"{_settings.BaseUrl}/instance/connect/{instanceName}"),
            "obter QR Code", instanceName);

        await EnsureSuccessAsync(response, "obter QR Code", instanceName);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        if (!body.TryGetProperty("base64", out var campo) ||
            campo.GetString() is not { Length: > 0 } base64)
        {
            throw new EvolutionApiException(
                $"QR Code não encontrado na resposta da Evolution para a instância '{instanceName}'.");
        }

        // Vem como data URI: "data:image/png;base64,iVBORw0..."
        var virgula = base64.IndexOf(',');
        var conteudo = virgula >= 0 ? base64[(virgula + 1)..] : base64;

        return Convert.FromBase64String(conteudo);
    }

    /// <summary>
    /// Lança em caso de falha. Quem chama grava o motivo real no status da mensagem —
    /// engolir a exceção aqui apagaria a única pista que o tenant tem para diagnosticar.
    /// </summary>
    public async Task<bool> SendMessageAsync(string instanceName, string phoneNumber, string message)
    {
        var payload = new { number = CleanPhoneNumber(phoneNumber), text = message };

        var response = await SendAsync(
            () => _httpClient.PostAsJsonAsync($"{_settings.BaseUrl}/message/sendText/{instanceName}", payload),
            "enviar mensagem", instanceName);

        await EnsureSuccessAsync(response, "enviar mensagem", instanceName);
        return true;
    }

    public async Task<string> GetConnectionStatusAsync(string instanceName)
    {
        var response = await SendAsync(
            () => _httpClient.GetAsync($"{_settings.BaseUrl}/instance/connectionState/{instanceName}"),
            "consultar status", instanceName);

        await EnsureSuccessAsync(response, "consultar status", instanceName);

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
        var response = await SendAsync(
            () => _httpClient.DeleteAsync($"{_settings.BaseUrl}/instance/logout/{instanceName}"),
            "desconectar instância", instanceName);

        await EnsureSuccessAsync(response, "desconectar instância", instanceName);
        _logger.LogInformation("Instância desconectada: {Instance}", instanceName);
        return true;
    }

    /// <summary>
    /// Remove a instância da Evolution. Diferente de <see cref="DisconnectInstanceAsync"/>,
    /// que só faz logout e deixa a instância existindo lá para sempre.
    /// </summary>
    public async Task<bool> DeleteInstanceAsync(string instanceName)
    {
        // A Evolution trava o delete de uma instância em "connecting"; deslogar antes
        // libera a sessão. O logout também pode travar, então vai com timeout curto:
        // é uma tentativa de melhor esforço, e esperar os 30s do HttpClient aqui
        // deixaria um DELETE simples levando meio minuto.
        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
        {
            try
            {
                await _httpClient.DeleteAsync(
                    $"{_settings.BaseUrl}/instance/logout/{instanceName}", cts.Token);
            }
            catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
            {
                _logger.LogDebug(ex,
                    "Logout antes do delete não completou para {Instance}, prosseguindo", instanceName);
            }
        }

        var response = await SendAsync(
            () => _httpClient.DeleteAsync($"{_settings.BaseUrl}/instance/delete/{instanceName}"),
            "remover instância", instanceName);

        // Já não existir na Evolution é o estado desejado, não um erro.
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogInformation("Instância já não existia na Evolution: {Instance}", instanceName);
            return true;
        }

        await EnsureSuccessAsync(response, "remover instância", instanceName);
        _logger.LogInformation("Instância removida da Evolution: {Instance}", instanceName);
        return true;
    }

    /// <summary>
    /// Executa a chamada convertendo falha de transporte (DNS, recusa de conexão,
    /// timeout) em EvolutionApiException. Sem isso, o modo de falha mais comum —
    /// o serviço simplesmente fora do ar — escapava como HttpRequestException crua
    /// e virava 500 em vez de 502.
    /// </summary>
    private async Task<HttpResponseMessage> SendAsync(
        Func<Task<HttpResponseMessage>> call, string operacao, string instanceName)
    {
        try
        {
            return await call();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex,
                "Evolution API inacessível ao {Operacao} '{Instance}'", operacao, instanceName);

            throw new EvolutionApiException(
                $"Evolution API inacessível ao {operacao} '{instanceName}': {ex.Message}",
                statusCode: null, responseBody: null, inner: ex);
        }
    }

    /// <summary>
    /// Substitui EnsureSuccessStatusCode(), que descarta o corpo da resposta — e é
    /// exatamente ali que a Evolution diz o motivo real da falha.
    /// </summary>
    private async Task<string> EnsureSuccessAsync(
        HttpResponseMessage response, string operacao, string instanceName)
    {
        var body = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
            return body;

        _logger.LogError(
            "Evolution API falhou ao {Operacao} '{Instance}': HTTP {Status}. Resposta: {Body}",
            operacao, instanceName, (int)response.StatusCode, body);

        throw new EvolutionApiException(
            $"Evolution API falhou ao {operacao} '{instanceName}' (HTTP {(int)response.StatusCode}): {body}",
            response.StatusCode,
            body);
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
