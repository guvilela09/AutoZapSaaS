using AutoZapSaaS.Application.Common.Interfaces;
using AutoZapSaaS.Application.DTOs;
using AutoZapSaaS.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoZapSaaS.API.Controllers;

/// <summary>
/// Onde o tenant obtém a URL de webhook para cadastrar na plataforma de vendas
/// e registra o secret que a plataforma usa para assinar as requisições.
/// </summary>
[ApiController]
[Authorize]
[Route("api/webhook-integrations")]
public class WebhookIntegrationsController : ControllerBase
{
    private readonly IWebhookIntegrationService _integrationService;
    private readonly ITenantContext _tenantContext;

    public WebhookIntegrationsController(
        IWebhookIntegrationService integrationService,
        ITenantContext tenantContext)
    {
        _integrationService = integrationService;
        _tenantContext = tenantContext;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWebhookIntegrationRequest request)
    {
        try
        {
            var result = await _integrationService.CreateAsync(_tenantContext.TenantId, request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _integrationService.GetByTenantAsync(_tenantContext.TenantId);
        return Ok(result);
    }

    [HttpPost("{id}/rotate-token")]
    public async Task<IActionResult> RotateToken(Guid id)
    {
        var result = await _integrationService.RotateTokenAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{id}/secret")]
    public async Task<IActionResult> UpdateSecret(Guid id, [FromBody] UpdateWebhookSecretRequest request)
    {
        var result = await _integrationService.UpdateSecretAsync(id, request);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _integrationService.DeleteAsync(id);
        return result ? NoContent() : NotFound();
    }
}
