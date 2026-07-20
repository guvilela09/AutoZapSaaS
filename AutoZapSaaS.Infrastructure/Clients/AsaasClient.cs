using System.Net.Http.Json;
using System.Text.Json;
using AutoZapSaaS.Application.Common;
using AutoZapSaaS.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoZapSaaS.Infrastructure.Clients;

public class AsaasSettings
{
    /// <summary>Sandbox: https://api-sandbox.asaas.com/v3 — Produção: https://api.asaas.com/v3</summary>
    public string BaseUrl { get; set; } = "https://api-sandbox.asaas.com/v3";
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Token que o Asaas envia no header do webhook, para autenticar a origem.</summary>
    public string WebhookToken { get; set; } = string.Empty;

    public bool Configurado => !string.IsNullOrWhiteSpace(ApiKey);
}

public class AsaasClient : IAsaasClient
{
    private readonly HttpClient _httpClient;
    private readonly AsaasSettings _settings;
    private readonly ILogger<AsaasClient> _logger;

    public AsaasClient(HttpClient httpClient, IOptions<AsaasSettings> settings, ILogger<AsaasClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<AsaasCliente> CriarOuAtualizarClienteAsync(
        string nome, string email, string cpfCnpj, CancellationToken ct = default)
    {
        GarantirConfigurado();

        var payload = new
        {
            name = nome,
            email,
            cpfCnpj = SomenteDigitos(cpfCnpj)
        };

        var response = await EnviarAsync(
            () => _httpClient.PostAsJsonAsync($"{_settings.BaseUrl}/customers", payload, ct),
            "criar cliente");

        var body = await LerSucessoAsync(response, "criar cliente");

        var id = body.GetProperty("id").GetString()
            ?? throw new AsaasException("Asaas nao retornou o id do cliente.");

        return new AsaasCliente(id);
    }

    public async Task<AsaasAssinatura> CriarAssinaturaAsync(
        string customerId, decimal valor, string descricao,
        AsaasFormaPagamento forma, CancellationToken ct = default)
    {
        GarantirConfigurado();

        var payload = new
        {
            customer = customerId,
            billingType = TraduzirForma(forma),
            value = valor,
            nextDueDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd"),
            cycle = "MONTHLY",
            description = descricao
        };

        var response = await EnviarAsync(
            () => _httpClient.PostAsJsonAsync($"{_settings.BaseUrl}/subscriptions", payload, ct),
            "criar assinatura");

        var body = await LerSucessoAsync(response, "criar assinatura");

        var id = body.GetProperty("id").GetString()
            ?? throw new AsaasException("Asaas nao retornou o id da assinatura.");

        DateTime? proximo = body.TryGetProperty("nextDueDate", out var nd)
            && DateTime.TryParse(nd.GetString(), out var data)
                ? data
                : null;

        return new AsaasAssinatura(id, proximo);
    }

    public async Task CancelarAssinaturaAsync(string subscriptionId, CancellationToken ct = default)
    {
        GarantirConfigurado();

        var response = await EnviarAsync(
            () => _httpClient.DeleteAsync($"{_settings.BaseUrl}/subscriptions/{subscriptionId}", ct),
            "cancelar assinatura");

        // Ja nao existir no Asaas e o estado desejado.
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogInformation("Assinatura {Id} ja nao existia no Asaas", subscriptionId);
            return;
        }

        await LerSucessoAsync(response, "cancelar assinatura");
    }

    private void GarantirConfigurado()
    {
        if (!_settings.Configurado)
        {
            throw new AsaasException(
                "Cobranca nao configurada: defina Asaas:ApiKey para habilitar planos pagos.");
        }
    }

    private static string TraduzirForma(AsaasFormaPagamento forma) => forma switch
    {
        AsaasFormaPagamento.Boleto => "BOLETO",
        AsaasFormaPagamento.CartaoCredito => "CREDIT_CARD",
        AsaasFormaPagamento.Pix => "PIX",
        _ => "UNDEFINED"
    };

    private static string SomenteDigitos(string valor) =>
        new(valor.Where(char.IsDigit).ToArray());

    /// <summary>Converte falha de transporte em AsaasException, como no client da Evolution.</summary>
    private async Task<HttpResponseMessage> EnviarAsync(
        Func<Task<HttpResponseMessage>> chamada, string operacao)
    {
        try
        {
            return await chamada();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Asaas inacessivel ao {Operacao}", operacao);
            throw new AsaasException($"Asaas inacessivel ao {operacao}: {ex.Message}", inner: ex);
        }
    }

    private async Task<JsonElement> LerSucessoAsync(HttpResponseMessage response, string operacao)
    {
        var corpo = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            // O Asaas descreve o erro no corpo; perder isso deixaria o suporte cego.
            _logger.LogError("Asaas falhou ao {Operacao}: HTTP {Status}. Resposta: {Body}",
                operacao, (int)response.StatusCode, corpo);

            throw new AsaasException(
                $"Asaas falhou ao {operacao} (HTTP {(int)response.StatusCode}): {corpo}");
        }

        return JsonDocument.Parse(corpo).RootElement;
    }
}
