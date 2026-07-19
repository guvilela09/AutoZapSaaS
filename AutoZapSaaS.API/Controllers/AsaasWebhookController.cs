using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AutoZapSaaS.Application.Services.Interfaces;
using AutoZapSaaS.Infrastructure.Clients;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AutoZapSaaS.API.Controllers;

/// <summary>
/// Endpoint publico de cobranca. O Asaas autentica enviando o token configurado
/// no painel dele pelo header asaas-access-token. Sem validar isso, qualquer um
/// poderia postar "pagamento confirmado" e liberar plano pago de graca.
/// </summary>
[ApiController]
[Route("api/webhooks/asaas")]
public class AsaasWebhookController : ControllerBase
{
    private readonly ISubscriptionService _subscriptions;
    private readonly AsaasSettings _settings;
    private readonly ILogger<AsaasWebhookController> _logger;

    public AsaasWebhookController(
        ISubscriptionService subscriptions,
        IOptions<AsaasSettings> settings,
        ILogger<AsaasWebhookController> logger)
    {
        _subscriptions = subscriptions;
        _settings = settings.Value;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Receber([FromBody] JsonElement payload)
    {
        if (!TokenValido())
        {
            _logger.LogWarning("Webhook do Asaas rejeitado: token invalido");
            return Unauthorized(new { error = "Webhook nao autorizado." });
        }

        var evento = payload.TryGetProperty("event", out var e) ? e.GetString() : null;
        if (string.IsNullOrWhiteSpace(evento))
            return BadRequest(new { error = "Evento ausente." });

        if (!payload.TryGetProperty("payment", out var pagamento))
        {
            _logger.LogInformation("Evento {Evento} do Asaas sem bloco payment, ignorado", evento);
            return Ok(new { received = true });
        }

        var subscriptionId = pagamento.TryGetProperty("subscription", out var s) ? s.GetString() : null;
        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            // Cobranca avulsa, nao ligada a assinatura: nao muda plano de ninguem.
            _logger.LogInformation("Evento {Evento} do Asaas sem assinatura, ignorado", evento);
            return Ok(new { received = true });
        }

        DateTime? proximoVencimento =
            pagamento.TryGetProperty("dueDate", out var dd) &&
            DateTime.TryParse(dd.GetString(), out var data)
                ? data.AddMonths(1)
                : null;

        try
        {
            await _subscriptions.AplicarEventoDeCobrancaAsync(evento, subscriptionId, proximoVencimento);
            return Ok(new { received = true });
        }
        catch (Exception ex)
        {
            // 500 faz o Asaas reenviar, que e o comportamento desejado numa falha nossa.
            _logger.LogError(ex, "Erro ao aplicar evento {Evento} do Asaas", evento);
            return StatusCode(500, new { error = "Erro ao processar evento." });
        }
    }

    private bool TokenValido()
    {
        if (string.IsNullOrWhiteSpace(_settings.WebhookToken))
        {
            // Sem token configurado o endpoint fica aberto — melhor recusar tudo
            // do que aceitar eventos de origem desconhecida.
            _logger.LogError("Asaas:WebhookToken nao configurado. Webhooks serao recusados.");
            return false;
        }

        var recebido = Request.Headers["asaas-access-token"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(recebido))
            return false;

        var a = Encoding.UTF8.GetBytes(recebido);
        var b = Encoding.UTF8.GetBytes(_settings.WebhookToken);

        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }
}
