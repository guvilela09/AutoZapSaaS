using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AutoZapSaaS.Web.Models;

namespace AutoZapSaaS.Web.Services;

/// <summary>
/// Falha vinda da API, ja traduzida para algo que a pagina pode mostrar ao lojista.
/// </summary>
public class ApiException : Exception
{
    public int StatusCode { get; }
    public string? PlanoAtual { get; }
    public string? PlanoSugerido { get; }

    public bool EhLimiteDePlano => StatusCode == (int)HttpStatusCode.PaymentRequired;
    public bool EhIndisponibilidade => StatusCode is 502 or 503 or 504;

    public ApiException(string message, int statusCode, string? planoAtual = null, string? planoSugerido = null)
        : base(message)
    {
        StatusCode = statusCode;
        PlanoAtual = planoAtual;
        PlanoSugerido = planoSugerido;
    }
}

/// <summary>
/// Unico ponto de contato do painel com a API. O JWT do lojista vem do cookie
/// de autenticacao e e anexado por requisicao — nunca guardado no cliente.
/// </summary>
public class AutoZapApiClient
{
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _contexto;
    private readonly ILogger<AutoZapApiClient> _logger;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public AutoZapApiClient(HttpClient http, IHttpContextAccessor contexto, ILogger<AutoZapApiClient> logger)
    {
        _http = http;
        _contexto = contexto;
        _logger = logger;
    }

    // ---------- Autenticacao ----------

    public Task<LoginResponse> LoginAsync(string email, string senha) => PostAnonimoAsync<LoginResponse>("/api/auth/login", new { email, password = senha });

    public Task<LoginResponse> RegistrarAsync(
        string nomeEmpresa, string emailEmpresa, string documento, string emailAdmin, string senhaAdmin) =>
        PostAnonimoAsync<LoginResponse>("/api/auth/register", new
        {
            name = nomeEmpresa,
            email = emailEmpresa,
            document = documento,
            adminEmail = emailAdmin,
            adminPassword = senhaAdmin
        });

    // ---------- Instancias ----------

    public Task<List<InstanceResponse>> ListarInstanciasAsync() => GetAsync<List<InstanceResponse>>("/api/instances");

    public Task<InstanceResponse> CriarInstanciaAsync(string nome, string sessao, string token) => PostAsync<InstanceResponse>("/api/instances", new { name = nome, sessionName = sessao, token });

    public Task<QrCodeResponse> ObterQrCodeAsync(Guid id) => GetAsync<QrCodeResponse>($"/api/instances/{id}/qrcode");

    public Task<ConnectionStatusResponse> ObterStatusAsync(Guid id) => GetAsync<ConnectionStatusResponse>($"/api/instances/{id}/status");

    public Task RemoverInstanciaAsync(Guid id) => DeleteAsync($"/api/instances/{id}");

    public Task DesconectarInstanciaAsync(Guid id) => PostSemRetornoAsync($"/api/instances/{id}/disconnect", new { });

    // ---------- Clientes ----------

    public Task<List<CustomerResponse>> ListarClientesAsync() => GetAsync<List<CustomerResponse>>("/api/customers");

    public Task<CustomerResponse> CriarClienteAsync(string nome, string telefone, string email, string origem) => PostAsync<CustomerResponse>("/api/customers", new { name = nome, phoneNumber = telefone, email, origin = origem });

    public Task RemoverClienteAsync(Guid id) => DeleteAsync($"/api/customers/{id}");

    // ---------- Templates ----------

    public Task<List<MessageTemplateResponse>> ListarTemplatesAsync() => GetAsync<List<MessageTemplateResponse>>("/api/templates");

    public Task<MessageTemplateResponse> CriarTemplateAsync(string nome, string evento, string corpo) => PostAsync<MessageTemplateResponse>("/api/templates", new { name = nome, eventType = evento, body = corpo });

    public Task RemoverTemplateAsync(Guid id) => DeleteAsync($"/api/templates/{id}");

    // ---------- Mensagens ----------

    public Task<List<WhatsAppMessageResponse>> ListarMensagensAsync() => GetAsync<List<WhatsAppMessageResponse>>("/api/whatsapp/messages");

    public Task EnviarMensagemAsync(Guid instanciaId, string telefone, string mensagem) => PostSemRetornoAsync("/api/whatsapp/send",new { instanceId = instanciaId, phoneNumber = telefone, message = mensagem });

    // ---------- Webhooks ----------

    public Task<List<WebhookIntegrationResponse>> ListarIntegracoesAsync() => GetAsync<List<WebhookIntegrationResponse>>("/api/webhook-integrations");

    public Task<WebhookIntegrationResponse> CriarIntegracaoAsync(string plataforma, string segredo) => PostAsync<WebhookIntegrationResponse>("/api/webhook-integrations", new { platform = plataforma, secret = segredo });

    public Task<WebhookIntegrationResponse> GirarTokenAsync(Guid id) => PostAsync<WebhookIntegrationResponse>($"/api/webhook-integrations/{id}/rotate-token", new { });

    public Task RemoverIntegracaoAsync(Guid id) => DeleteAsync($"/api/webhook-integrations/{id}");

    // ---------- Assinatura ----------

    public Task<List<PlanoResponse>> ListarPlanosAsync() => GetAsync<List<PlanoResponse>>("/api/subscription/plans");

    public Task<AssinaturaResponse> ObterAssinaturaAsync() => GetAsync<AssinaturaResponse>("/api/subscription");

    public Task<AssinaturaResponse> AssinarAsync(string tier, string formaPagamento) => PostAsync<AssinaturaResponse>("/api/subscription", new { tier, formaPagamento });

    public Task<AssinaturaResponse> CancelarAssinaturaAsync() => DeleteAsync<AssinaturaResponse>("/api/subscription");

    // ---------- Infra ----------

    private void Autenticar(HttpRequestMessage req)
    {
        var token = _contexto.HttpContext?.User.FindFirst("jwt")?.Value;

        if (!string.IsNullOrWhiteSpace(token))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<T> GetAsync<T>(string rota)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, rota);
        Autenticar(req);
        return await EnviarAsync<T>(req);
    }

    private async Task<T> PostAsync<T>(string rota, object corpo)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, rota)
        {
            Content = JsonContent.Create(corpo)
        };
        Autenticar(req);
        return await EnviarAsync<T>(req);
    }

    private async Task PostSemRetornoAsync(string rota, object corpo)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, rota)
        {
            Content = JsonContent.Create(corpo)
        };
        Autenticar(req);
        await EnviarBrutoAsync(req);
    }

    private async Task<T> PostAnonimoAsync<T>(string rota, object corpo)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, rota)
        {
            Content = JsonContent.Create(corpo)
        };
        return await EnviarAsync<T>(req);
    }

    private async Task DeleteAsync(string rota)
    {
        using var req = new HttpRequestMessage(HttpMethod.Delete, rota);
        Autenticar(req);
        await EnviarBrutoAsync(req);
    }

    private async Task<T> DeleteAsync<T>(string rota)
    {
        using var req = new HttpRequestMessage(HttpMethod.Delete, rota);
        Autenticar(req);
        return await EnviarAsync<T>(req);
    }

    private async Task<T> EnviarAsync<T>(HttpRequestMessage req)
    {
        var corpo = await EnviarBrutoAsync(req);

        return JsonSerializer.Deserialize<T>(corpo, Json)
            ?? throw new ApiException("A API devolveu uma resposta vazia.", 500);
    }

    private async Task<string> EnviarBrutoAsync(HttpRequestMessage req)
    {
        HttpResponseMessage resposta;

        try
        {
            resposta = await _http.SendAsync(req);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "API inacessivel em {Rota}", req.RequestUri);
            throw new ApiException("Nao foi possivel falar com o servidor. Tente novamente.", 503);
        }

        var corpo = await resposta.Content.ReadAsStringAsync();

        if (resposta.IsSuccessStatusCode)
            return corpo;

        throw TraduzirErro(resposta.StatusCode, corpo);
    }

    /// <summary>
    /// Converte o corpo de erro da API em algo exibivel. O 402 carrega o plano
    /// sugerido, para a pagina oferecer o upgrade em vez de so mostrar um erro.
    /// </summary>
    private ApiException TraduzirErro(HttpStatusCode status, string corpo)
    {
        var codigo = (int)status;

        try
        {
            var erro = JsonSerializer.Deserialize<ApiErro>(corpo, Json);

            if (!string.IsNullOrWhiteSpace(erro?.Error))
                return new ApiException(erro.Error, codigo, erro.PlanoAtual, erro.PlanoSugerido);

            // Falha de validacao vem como ValidationProblemDetails, com as mensagens
            // dentro de "errors" e nao em "error". Sem tratar esse formato, o lojista
            // veria "erro inesperado" no lugar de "a senha precisa ter 8 caracteres".
            var mensagensDeValidacao = ExtrairMensagensDeValidacao(corpo);
            if (mensagensDeValidacao is not null)
                return new ApiException(mensagensDeValidacao, codigo);
        }
        catch (JsonException)
        {
            // Resposta nao-JSON (ex.: pagina de erro do servidor): cai na mensagem generica.
        }

        var mensagem = status switch
        {
            HttpStatusCode.Unauthorized => "Sessao expirada. Entre novamente.",
            HttpStatusCode.NotFound => "Registro nao encontrado.",
            HttpStatusCode.BadGateway => "Servico indisponivel no momento. Tente novamente.",
            _ => "Ocorreu um erro inesperado. Tente novamente."
        };

        return new ApiException(mensagem, codigo);
    }

    /// <summary>
    /// Junta as mensagens de ValidationProblemDetails numa frase so. Devolve null
    /// quando o corpo nao tem esse formato.
    /// </summary>
    private static string? ExtrairMensagensDeValidacao(string corpo)
    {
        using var documento = JsonDocument.Parse(corpo);

        if (!documento.RootElement.TryGetProperty("errors", out var erros) ||
            erros.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var mensagens = erros.EnumerateObject()
            .SelectMany(campo => campo.Value.ValueKind == JsonValueKind.Array
                ? campo.Value.EnumerateArray().Select(m => m.GetString())
                : new[] { campo.Value.GetString() })
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Distinct()
            .ToList();

        return mensagens.Count > 0 ? string.Join(" ", mensagens) : null;
    }
}
