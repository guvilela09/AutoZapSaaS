using AutoZapSaaS.Application.Common.Interfaces;
using AutoZapSaaS.Application.DTOs;
using AutoZapSaaS.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoZapSaaS.API.Controllers;

[ApiController]
[Authorize]
[Route("api/whatsapp")]
public class WhatsAppController : ControllerBase
{
    private readonly IWhatsAppService _whatsAppService;
    private readonly ITenantContext _tenantContext;

    public WhatsAppController(IWhatsAppService whatsAppService, ITenantContext tenantContext)
    {
        _whatsAppService = whatsAppService;
        _tenantContext = tenantContext;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        try
        {
            var result = await _whatsAppService.SendMessageAsync(_tenantContext.TenantId, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("send-template")]
    public async Task<IActionResult> SendTemplate([FromBody] SendTemplateMessageRequest request)
    {
        try
        {
            var result = await _whatsAppService.SendTemplateAsync(_tenantContext.TenantId, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("messages")]
    public async Task<IActionResult> GetMessages()
    {
        var messages = await _whatsAppService.GetMessagesAsync(_tenantContext.TenantId);
        return Ok(messages);
    }

    [HttpPost("process-pending")]
    public async Task<IActionResult> ProcessPending()
    {
        await _whatsAppService.ProcessPendingMessagesAsync();
        return Ok(new { message = "Mensagens pendentes processadas." });
    }
}
