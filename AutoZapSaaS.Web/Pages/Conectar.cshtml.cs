using AutoZapSaaS.Web.Models;
using AutoZapSaaS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutoZapSaaS.Web.Pages;

public class ConectarModel : PageModel
{
    private readonly AutoZapApiClient _api;
    private readonly ILogger<ConectarModel> _logger;

    public ConectarModel(AutoZapApiClient api, ILogger<ConectarModel> logger)
    {
        _api = api;
        _logger = logger;
    }

    public InstanceResponse? Instancia { get; private set; }
    public string? QrCodeBase64 { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            Instancia = (await _api.ListarInstanciasAsync()).FirstOrDefault(i => i.Id == id);
        }
        catch (ApiException ex)
        {
            TempData["Erro"] = ex.Message;
            return RedirectToPage("/Instancias");
        }

        if (Instancia is null)
        {
            TempData["Erro"] = "Número não encontrado.";
            return RedirectToPage("/Instancias");
        }

        try
        {
            QrCodeBase64 = (await _api.ObterQrCodeAsync(id)).QrCodeBase64;
        }
        catch (ApiException ex)
        {
            TempData["Erro"] = ex.Message;
        }

        return Page();
    }

    /// <summary>
    /// Consultado por JavaScript a cada poucos segundos: o lojista escaneia o QR
    /// e a tela avança sozinha, sem precisar recarregar na mão.
    /// </summary>
    public async Task<IActionResult> OnGetStatusAsync(Guid id)
    {
        try
        {
            var status = await _api.ObterStatusAsync(id);
            return new JsonResult(new { status = status.Status });
        }
        catch (ApiException ex)
        {
            _logger.LogWarning("Falha ao consultar status de {Id}: {Msg}", id, ex.Message);
            return new JsonResult(new { status = "desconhecido" });
        }
    }
}
