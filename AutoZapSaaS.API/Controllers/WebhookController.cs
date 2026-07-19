using System.Text.Json;
using AutoZapSaaS.Application.Common.Interfaces;
using AutoZapSaaS.Application.Services.Interfaces;
using AutoZapSaaS.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AutoZapSaaS.API.Controllers;

/// <summary>
/// Endpoint público. A identidade do tenant vem do token na URL e a autenticidade
/// da requisição vem da assinatura HMAC — nunca de um header que o chamador escolhe.
/// </summary>
[ApiController]
[Route("api/webhooks")]
public class WebhookController : ControllerBase
{
    private readonly IWebhookService _webhookService;
    private readonly IWebhookIntegrationService _integrationService;
    private readonly IWebhookSignatureValidator _signatureValidator;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(
        IWebhookService webhookService,
        IWebhookIntegrationService integrationService,
        IWebhookSignatureValidator signatureValidator,
        ITenantContext tenantContext,
        ILogger<WebhookController> logger)
    {
        _webhookService = webhookService;
        _integrationService = integrationService;
        _signatureValidator = signatureValidator;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    [HttpPost("kiwify/{token}")]
    public Task<IActionResult> ReceiveKiwify(string token) =>
        HandleAsync(token, WebhookPlatform.Kiwify,
            async (tenantId, payload) => await _webhookService.ProcessKiwifyAsync(tenantId, payload));

    [HttpPost("hotmart/{token}")]
    public Task<IActionResult> ReceiveHotmart(string token) =>
        HandleAsync(token, WebhookPlatform.Hotmart,
            async (tenantId, payload) => await _webhookService.ProcessHotmartAsync(tenantId, payload));

    [HttpPost("nuvemshop/{token}")]
    public Task<IActionResult> ReceiveNuvemshop(string token) =>
        HandleAsync(token, WebhookPlatform.Nuvemshop,
            async (tenantId, payload) => await _webhookService.ProcessNuvemshopAsync(tenantId, payload));

    private async Task<IActionResult> HandleAsync(
        string token,
        WebhookPlatform expectedPlatform,
        Func<Guid, JsonElement, Task<object>> process)
    {
        var integration = await _integrationService.ResolveByTokenAsync(token);

        // Token inválido e plataforma trocada respondem igual a assinatura inválida:
        // não entregamos ao atacante a informação de qual token existe.
        if (integration is null || integration.Platform != expectedPlatform)
        {
            _logger.LogWarning("Webhook rejeitado: token desconhecido para {Platform}", expectedPlatform);
            return Unauthorized(new { error = "Webhook não autorizado." });
        }

        var rawBody = await ReadRawBodyAsync();

        if (!_signatureValidator.IsValid(expectedPlatform, rawBody, ExtractSignature(expectedPlatform), integration.Secret))
        {
            _logger.LogWarning("Webhook rejeitado: assinatura inválida para tenant {TenantId} ({Platform})",
                integration.TenantId, expectedPlatform);
            return Unauthorized(new { error = "Webhook não autorizado." });
        }

        // Só depois de provar a origem é que a requisição ganha um tenant.
        _tenantContext.SetTenant(integration.TenantId);

        JsonElement payload;
        try
        {
            payload = JsonDocument.Parse(rawBody).RootElement;
        }
        catch (JsonException)
        {
            return BadRequest(new { error = "Payload não é um JSON válido." });
        }

        try
        {
            var result = await process(integration.TenantId, payload);
            await _integrationService.MarkReceivedAsync(integration.IntegrationId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            // A mensagem crua da exceção pode conter detalhe interno — fica só no log.
            _logger.LogError(ex, "Erro ao processar webhook {Platform} do tenant {TenantId}",
                expectedPlatform, integration.TenantId);
            return StatusCode(500, new { error = "Erro ao processar webhook." });
        }
    }

    /// <summary>Corpo exatamente como chegou — reserializar invalidaria o HMAC.</summary>
    private async Task<string> ReadRawBodyAsync()
    {
        Request.EnableBuffering();
        Request.Body.Position = 0;

        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();

        Request.Body.Position = 0;
        return body;
    }

    private string? ExtractSignature(WebhookPlatform platform) => platform switch
    {
        WebhookPlatform.Kiwify => Request.Query["signature"].FirstOrDefault(),
        WebhookPlatform.Hotmart => Request.Headers["X-HOTMART-HOTTOK"].FirstOrDefault(),
        WebhookPlatform.Nuvemshop => Request.Headers["x-linkedstore-hmac-sha256"].FirstOrDefault(),
        _ => null
    };
}
