using AutoZapSaaS.Application.Common;
using AutoZapSaaS.Application.Common.Interfaces;
using AutoZapSaaS.Application.DTOs;
using AutoZapSaaS.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoZapSaaS.API.Controllers;

[ApiController]
[Authorize]
[Route("api/instances")]
public class InstancesController : ControllerBase
{
    private readonly IInstanceService _instanceService;
    private readonly ITenantContext _tenantContext;

    public InstancesController(IInstanceService instanceService, ITenantContext tenantContext)
    {
        _instanceService = instanceService;
        _tenantContext = tenantContext;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInstanceRequest request)
    {
        try
        {
            var result = await _instanceService.CreateAsync(_tenantContext.TenantId, request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (EvolutionApiException ex)
        {
            return EvolutionIndisponivel(ex);
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetByTenant()
    {
        var instances = await _instanceService.GetByTenantAsync(_tenantContext.TenantId);
        return Ok(instances);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var instance = await _instanceService.GetByIdAsync(id);
        return instance is null ? NotFound() : Ok(instance);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInstanceRequest request)
    {
        var result = await _instanceService.UpdateAsync(id, request);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _instanceService.DeleteAsync(id);
        return result ? NoContent() : NotFound();
    }

    [HttpPost("{id}/connect")]
    public async Task<IActionResult> Connect(Guid id)
    {
        try
        {
            var result = await _instanceService.ConnectAsync(id);
            return result ? Ok(new { message = "Conectando..." }) : NotFound();
        }
        catch (EvolutionApiException ex)
        {
            return EvolutionIndisponivel(ex);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/disconnect")]
    public async Task<IActionResult> Disconnect(Guid id)
    {
        try
        {
            var result = await _instanceService.DisconnectAsync(id);
            return result ? Ok(new { message = "Desconectado." }) : NotFound();
        }
        catch (EvolutionApiException ex)
        {
            return EvolutionIndisponivel(ex);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpGet("{id}/qrcode")]
    public async Task<IActionResult> GetQrCode(Guid id)
    {
        try
        {
            var qrCode = await _instanceService.GetQrCodeAsync(id);
            return Ok(qrCode);
        }
        catch (EvolutionApiException ex)
        {
            return EvolutionIndisponivel(ex);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpGet("{id}/status")]
    public async Task<IActionResult> GetStatus(Guid id)
    {
        try
        {
            var status = await _instanceService.GetStatusAsync(id);
            return Ok(status);
        }
        catch (EvolutionApiException ex)
        {
            return EvolutionIndisponivel(ex);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 502: a falha e da Evolution, nao da requisicao do tenant. Devolver 400 aqui
    /// culparia o cliente por uma indisponibilidade nossa. O corpo cru da resposta
    /// fica so no log, para nao vazar detalhe interno.
    /// </summary>
    private IActionResult EvolutionIndisponivel(EvolutionApiException ex) =>
        StatusCode(StatusCodes.Status502BadGateway, new
        {
            error = "Nao foi possivel falar com o servico de WhatsApp. Tente novamente.",
            upstreamStatus = ex.StatusCode is null ? null : (int?)ex.StatusCode
        });
}
