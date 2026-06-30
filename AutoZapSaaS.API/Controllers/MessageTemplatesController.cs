using AutoZapSaaS.API.Common;
using AutoZapSaaS.Application.DTOs;
using AutoZapSaaS.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoZapSaaS.API.Controllers;

[ApiController]
[Authorize]
[Route("api/templates")]
public class MessageTemplatesController : ControllerBase
{
    private readonly IMessageTemplateService _templateService;
    private readonly ITenantContext _tenantContext;

    public MessageTemplatesController(IMessageTemplateService templateService, ITenantContext tenantContext)
    {
        _templateService = templateService;
        _tenantContext = tenantContext;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMessageTemplateRequest request)
    {
        try
        {
            var result = await _templateService.CreateAsync(_tenantContext.TenantId, request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var templates = await _templateService.GetByTenantAsync(_tenantContext.TenantId);
        return Ok(templates);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var template = await _templateService.GetByIdAsync(id);
        return template is null ? NotFound() : Ok(template);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMessageTemplateRequest request)
    {
        try
        {
            var result = await _templateService.UpdateAsync(id, request);
            return result is null ? NotFound() : Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _templateService.DeleteAsync(id);
        return result ? NoContent() : NotFound();
    }
}
