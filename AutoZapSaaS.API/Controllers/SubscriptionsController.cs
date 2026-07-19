using AutoZapSaaS.Application.Common;
using AutoZapSaaS.Application.Common.Interfaces;
using AutoZapSaaS.Application.DTOs;
using AutoZapSaaS.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoZapSaaS.API.Controllers;

[ApiController]
[Authorize]
[Route("api/subscription")]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptions;
    private readonly ITenantContext _tenantContext;

    public SubscriptionsController(ISubscriptionService subscriptions, ITenantContext tenantContext)
    {
        _subscriptions = subscriptions;
        _tenantContext = tenantContext;
    }

    [HttpGet("plans")]
    public async Task<IActionResult> Planos() =>
        Ok(await _subscriptions.ListarPlanosAsync(_tenantContext.TenantId));

    [HttpGet]
    public async Task<IActionResult> Atual() =>
        Ok(await _subscriptions.ObterAtualAsync(_tenantContext.TenantId));

    [HttpPost]
    public async Task<IActionResult> Assinar([FromBody] AssinarRequest request)
    {
        try
        {
            return Ok(await _subscriptions.AssinarAsync(_tenantContext.TenantId, request));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (AsaasException)
        {
            // Falha da cobranca e nossa, nao do pedido do lojista.
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                error = "Nao foi possivel iniciar a cobranca agora. Tente novamente."
            });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> Cancelar()
    {
        try
        {
            return Ok(await _subscriptions.CancelarAsync(_tenantContext.TenantId));
        }
        catch (AsaasException)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                error = "Nao foi possivel cancelar a cobranca agora. Tente novamente."
            });
        }
    }
}
