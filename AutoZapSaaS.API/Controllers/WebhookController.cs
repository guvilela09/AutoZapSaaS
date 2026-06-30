using System.Text.Json;
using AutoZapSaaS.Application.Services.Interfaces;
using AutoZapSaaS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoZapSaaS.API.Controllers;

[ApiController]
[Route("api/webhooks")]
public class WebhookController : ControllerBase
{
    private readonly IWebhookService _webhookService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(
        IWebhookService webhookService,
        ApplicationDbContext context,
        ILogger<WebhookController> logger)
    {
        _webhookService = webhookService;
        _context = context;
        _logger = logger;
    }

    [HttpPost("kiwify")]
    public async Task<IActionResult> ReceiveKiwify([FromBody] JsonElement payload, [FromHeader] string? xTenantId)
    {
        try
        {
            var tenantId = await ResolveTenantAsync(xTenantId);

            var result = await _webhookService.ProcessKiwifyAsync(tenantId, payload);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro webhook Kiwify");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("hotmart")]
    public async Task<IActionResult> ReceiveHotmart([FromBody] JsonElement payload, [FromHeader] string? xTenantId)
    {
        try
        {
            var tenantId = await ResolveTenantAsync(xTenantId);

            var result = await _webhookService.ProcessHotmartAsync(tenantId, payload);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro webhook Hotmart");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("nuvemshop")]
    public async Task<IActionResult> ReceiveNuvemshop([FromBody] JsonElement payload, [FromHeader] string? xTenantId)
    {
        try
        {
            var tenantId = await ResolveTenantAsync(xTenantId);

            var result = await _webhookService.ProcessNuvemshopAsync(tenantId, payload);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro webhook Nuvemshop");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("receive")]
    public async Task<IActionResult> ReceiveWebhook([FromBody] JsonElement payload)
    {
        try
        {
            var tenant = await _context.Tenants.FirstOrDefaultAsync();
            if (tenant is null)
                return BadRequest("Nenhum tenant encontrado.");

            var result = await _webhookService.ProcessKiwifyAsync(tenant.Id, payload);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro webhook genérico");
            return BadRequest(new { error = ex.Message });
        }
    }

    private async Task<Guid> ResolveTenantAsync(string? xTenantId)
    {
        if (!string.IsNullOrWhiteSpace(xTenantId) && Guid.TryParse(xTenantId, out var tid))
            return tid;

        var tenant = await _context.Tenants.FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Nenhum tenant encontrado. Informe X-Tenant-Id.");

        return tenant.Id;
    }
}
