using AutoZapSaaS.Web.Models;
using AutoZapSaaS.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoZapSaaS.Web.Controllers;

public class PlanosController : Controller
{
    private readonly AutoZapApiClient _api;

    public PlanosController(AutoZapApiClient api) => _api = api;

    public async Task<IActionResult> Index()
    {
        var modelo = new PlanosViewModel();

        try
        {
            modelo.Planos = await _api.ListarPlanosAsync();
            modelo.Assinatura = await _api.ObterAssinaturaAsync();
        }
        catch (ApiException ex)
        {
            TempData["Erro"] = ex.Message;
        }

        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assinar(string tier, string formaPagamento)
    {
        try
        {
            await _api.AssinarAsync(tier, formaPagamento);
            TempData["Sucesso"] = "Assinatura criada. Assim que o pagamento for confirmado, " +
                                  "os novos limites entram em vigor.";
        }
        catch (ApiException ex)
        {
            TempData["Erro"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar()
    {
        try
        {
            await _api.CancelarAssinaturaAsync();
            TempData["Sucesso"] = "Assinatura cancelada. Sua conta voltou para o plano Free.";
        }
        catch (ApiException ex)
        {
            TempData["Erro"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
